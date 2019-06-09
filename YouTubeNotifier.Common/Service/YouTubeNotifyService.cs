using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using YouTubeNotifier.Common.Repository;

namespace YouTubeNotifier.Common.Service
{
    public class YouTubeNotifyService
    {
        private readonly YouTubeNotifyServiceConfig config;
        private readonly SubscriptionChannelRepository subscriptionChannelRepository;
        private readonly IMyLogger log;
        private YouTubeService youTubeService;

        public YouTubeNotifyService(
            YouTubeNotifyServiceConfig config,
            SubscriptionChannelRepository subscriptionChannelRepository,
            IMyLogger log
        )
        {
            this.config = config;
            this.subscriptionChannelRepository = subscriptionChannelRepository;
            this.log = log;
        }

        public async Task Run()
        {
            log.Infomation("CreateYoutubeService");
            youTubeService = await YoutubeServiceCreator.Create(config);

            var fromDateTimeJst = config.FromDateTimeUtc.AddHours(9);
            log.Infomation($"fromDateTimeJst={fromDateTimeJst}");

            log.Infomation($"GetOrInsertPlaylist({fromDateTimeJst})");
            var insertPlaylistResponse = await GetOrInsertPlaylist(fromDateTimeJst);

            if (config.UseCache)
            {
                throw new NotSupportedException(nameof(config.UseCache));
            }
            log.Infomation($"GetSubscriptionYouTubeChannels");
            var targetYouTubeChannelIds = await subscriptionChannelRepository.GetByCategory("MyAccountSubscription");

            log.Infomation($"targetYouTubeChannelIds.Count={targetYouTubeChannelIds.Length}");

            var fromUtc = config.FromDateTimeUtc;
            var toUtc = config.ToDateTimeUtc;

            var videoInfos = await GetUploadedVideos(targetYouTubeChannelIds, fromUtc, toUtc);
            log.Infomation($"movieIds.Count={videoInfos.Count}");

            log.Infomation($"Insert Movies");
            foreach (var videoInfo in videoInfos)
            {
                var insertPlaylistItemRequest = youTubeService.PlaylistItems.Insert(new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = insertPlaylistResponse.Id,
                        ResourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = videoInfo.videoId,
                        }
                    },
                }, "snippet");

                insertPlaylistItemRequest.Fields = "";
                log.Infomation($"insertPlaylistItemRequest VideoId={videoInfo}");

                await insertPlaylistItemRequest.ExecuteAsync();
            }
        }

        private async Task<List<(DateTimeOffset publishDate, string title, string videoId)>> GetUploadedVideos(SubscriptionChannelInfo[] targetYouTubeChannelIds, DateTime fromUtc, DateTime toUtc)
        {
            var movieIds = new List<(DateTimeOffset publishDate, string title, string videoId)>();

            using (var httpClient = new HttpClient())
            {
                foreach (var channelInfo in targetYouTubeChannelIds)
                {
                    log.Infomation($"GetUploadedMovies({channelInfo.YouTubeChannelId}, {fromUtc}, {toUtc})");

                    var url = $"https://www.youtube.com/feeds/videos.xml?channel_id={channelInfo.YouTubeChannelId}";

                    var rssContent = await httpClient.GetStringAsync(url);

                    using (var memoryStream = rssContent.ToMemoryStream(Encoding.UTF8))
                    using (var xmlReader = XmlReader.Create(memoryStream))
                    {
                        var syndicationFeed = SyndicationFeed.Load(xmlReader);

                        var items = syndicationFeed.Items
                            .Where(x => x.PublishDate.AddHours(9) >= fromUtc)
                            .Where(x => x.PublishDate.AddHours(9) <= toUtc);

                        foreach (var item in items)
                        {
                            var movieId = item.Links.First().Uri.Query.Split('=').Last();

                            movieIds.Add((item.PublishDate, item.Title.Text, movieId));

                            log.Infomation($"publishedDate={item.PublishDate} title={item.Title.Text} uri={item.Links.First().Uri} movieId={movieId}");
                        }
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            return movieIds;
        }

        private async Task<Playlist> GetOrInsertPlaylist(DateTime fromDateTimeJst)
        {
            var pageToken = default(string);

            var playlistTitle = fromDateTimeJst.ToString("yyyy年M月dd日");

            var page = 0;
            var maxPage = 1;

            do
            {
                var listPlaylistRequest = youTubeService.Playlists.List("id,snippet");
                listPlaylistRequest.Mine = true;
                listPlaylistRequest.MaxResults = 5;
                listPlaylistRequest.PageToken = pageToken;
                listPlaylistRequest.Fields = "nextPageToken,items/id,items/snippet/title";

                log.Infomation("listPlaylistRequest.ExecuteAsync");
                var listPlaylistResponse = await listPlaylistRequest.ExecuteAsync();

                var playlist = listPlaylistResponse.Items
                    .Where(x => x.Snippet.Title == playlistTitle)
                    .FirstOrDefault();

                if (playlist != null)
                {
                    return playlist;
                }

                pageToken = listPlaylistResponse.NextPageToken;
                page++;

                log.Infomation($"listPlaylistResponse.NextPageToken={pageToken}, page={page}");
                log.Infomation($")!string.IsNullOrEmpty({pageToken}) && {page} <= {maxPage}) == {!string.IsNullOrEmpty(pageToken) && page <= maxPage}");
            } while (!string.IsNullOrEmpty(pageToken) && page <= maxPage);

            // not found playlist. insert playlist.

            var insertPlaylistRequest = youTubeService.Playlists.Insert(new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = playlistTitle,
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "private",
                },
            }, "snippet,status");

            insertPlaylistRequest.Fields = "id";

            log.Infomation("insertPlaylistRequest.ExecuteAsync");
            var insertPlaylistResponse = await insertPlaylistRequest.ExecuteAsync();
            return insertPlaylistResponse;
        }

        private async Task<List<string>> GetUploadedVideoUsedApi(string channelId, DateTime from, DateTime to)
        {
            var list = new List<string>();

            var pageToken = default(string);

            do
            {
                var searchRequest = youTubeService.Search.List("id");
                searchRequest.ChannelId = channelId;
                searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchRequest.MaxResults = 5;
                searchRequest.Fields = "nextPageToken,items/id/kind,items/id/videoId";
                searchRequest.PublishedAfter = from;
                searchRequest.PublishedBefore = to;
                searchRequest.Type = "video";
                searchRequest.PageToken = pageToken;

                log.Infomation("searchRequest.ExecuteAsync");
                var searchResponse = await searchRequest.ExecuteAsync();

                foreach (var item in searchResponse.Items)
                {
                    if (item.Id.Kind != "youtube#video")
                    {
                        continue;
                    }

                    list.Add(item.Id.VideoId);
                }

                pageToken = searchResponse.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return list;
        }
    }
}
