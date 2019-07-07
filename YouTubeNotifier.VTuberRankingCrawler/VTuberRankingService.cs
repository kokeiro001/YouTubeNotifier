using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using YouTubeNotifier.Common.Service;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class VTuberRankingService
    {
        private readonly string azureCloudStorageConnectionString;
        private readonly YouTubeBlobService youtubeBlobService;
        private readonly Log4NetLogger log;

        public VTuberRankingService(Settings settings)
        {
            azureCloudStorageConnectionString = settings.AzureCloudStorageConnectionString;
            youtubeBlobService = new YouTubeBlobService(azureCloudStorageConnectionString);

            log = Log4NetLogger.Create();
        }

        public async Task GetNewMovies()
        {
            log.Infomation("GetNewMovies");

            var vtuberInsightCrawler = new VTuberInsightCrawler();

            log.Infomation("vtuberInsightCrawler.Run()");

            var rankingItems = await vtuberInsightCrawler.Run();

            log.Infomation($"rankingItems.Length={rankingItems.Length}");

            foreach (var rankingItem in rankingItems)
            {
                log.Infomation($"{rankingItem.Rank:D3} {rankingItem.ChannelName} {rankingItem.ChannelId}");
            }

            await youtubeBlobService.UploadVTuberInsightCsvFile(rankingItems);

            var fromUtc = DateTime.UtcNow.AddHours(9).Date.AddDays(-1).AddHours(-9);
            var toUtc = fromUtc.AddDays(1);

            var latestYouYubeRssItems = await GetMovieIds(fromUtc, toUtc);

            await youtubeBlobService.UploadLatestYouTubeMovies(latestYouYubeRssItems);
        }

        private async Task<YouTubeRssItem[]> GetMovieIds(DateTime fromUtc, DateTime toUtc)
        {
            var content = await youtubeBlobService.DownloadLatestVTuberInsightCsvFile();
            var rankingItems = JsonConvert.DeserializeObject<YouTubeChannelRankingItem[]>(content);

            var youtubeChannelRssCrawler = new YouTubeChannelRssCrawler();
            var youtubeChannelIds = rankingItems.Select(x => x.ChannelId).ToArray();

            return await youtubeChannelRssCrawler.GetUploadedMovies(youtubeChannelIds, fromUtc, toUtc);
        }

        public async Task GeneratePlaylistFromLatestMoviesJson()
        {
            log.Infomation("GeneratePlaylistFromLatestMoviesJson");

            var newMovies = await youtubeBlobService.DownloadLatestYouTubeMovies();

            log.Infomation($"newMovies.Length={newMovies.Length}");

            var youTubeService = await YoutubeServiceCreator.Create(azureCloudStorageConnectionString);

            var fromDateTimeJst = DateTime.UtcNow.AddHours(9).Date.AddDays(-1).AddHours(-9);
            log.Infomation($"GetOrInsertPlaylist(youTubeService, {fromDateTimeJst})");
            var insertPlaylistResponse = await GetOrInsertPlaylist(youTubeService, fromDateTimeJst);

            log.Infomation($"Insert Movies");
            foreach (var movie in newMovies)
            {
                var insertPlaylistItemRequest = youTubeService.PlaylistItems.Insert(new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = insertPlaylistResponse.Id,
                        ResourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = movie.MovieId,
                        }
                    },
                }, "snippet");

                insertPlaylistItemRequest.Fields = "";
                log.Infomation($"insertPlaylistItemRequest VideoId={movie.MovieId}");

                await insertPlaylistItemRequest.ExecuteAsync();
            }
        }

        private async Task<Playlist> GetOrInsertPlaylist(YouTubeService youTubeService, DateTime fromDateTimeJst)
        {
            var pageToken = default(string);

            var playlistTitle = fromDateTimeJst.ToString("yyyy年M月dd日");

            log.Infomation($"GetOrInsertPlaylist playlistTitle={playlistTitle}");

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
                log.Infomation($"!string.IsNullOrEmpty({pageToken}) && {page} <= {maxPage}) == {!string.IsNullOrEmpty(pageToken) && page <= maxPage}");
            } while (!string.IsNullOrEmpty(pageToken) && page <= maxPage);

            // not found playlist. insert playlist.
            log.Infomation($"NotFound playlistTitle={playlistTitle}");

            log.Infomation($"InsertPlayList {playlistTitle}");

            var insertPlaylistRequest = youTubeService.Playlists.Insert(new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = playlistTitle,
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "unlisted",
                },
            }, "snippet,status");

            insertPlaylistRequest.Fields = "id";

            log.Infomation("insertPlaylistRequest.ExecuteAsync");
            var insertPlaylistResponse = await insertPlaylistRequest.ExecuteAsync();
            return insertPlaylistResponse;
        }
    }
}
