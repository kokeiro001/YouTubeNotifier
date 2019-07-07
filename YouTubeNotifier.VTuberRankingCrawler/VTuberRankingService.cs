using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class VTuberRankingService
    {
        private readonly YouTubeService youtubeService;
        private readonly YouTubeBlobService youtubeBlobService;
        private readonly VTuberInsightCrawler vtuberInsightCrawler;
        private readonly YouTubeChannelRssCrawler youtubeChannelRssCrawler;
        private readonly Log4NetLogger log;

        public VTuberRankingService(
            YouTubeService youtubeService,
            YouTubeBlobService youtubeBlobService,
            VTuberInsightCrawler vtuberInsightCrawler,
            YouTubeChannelRssCrawler youtubeChannelRssCrawler,
            Log4NetLogger log)
        {
            this.youtubeService = youtubeService;
            this.youtubeBlobService = youtubeBlobService;
            this.vtuberInsightCrawler = vtuberInsightCrawler;
            this.youtubeChannelRssCrawler = youtubeChannelRssCrawler;
            this.log = log;
        }

        public async Task GetNewMovies()
        {
            log.Infomation("GetNewMovies");

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

            await youtubeBlobService.UploadLatestYouTubeMovies(fromUtc, toUtc, latestYouYubeRssItems);
        }

        private async Task<YouTubeRssItem[]> GetMovieIds(DateTime fromUtc, DateTime toUtc)
        {
            var content = await youtubeBlobService.DownloadLatestVTuberInsightCsvFile();
            var rankingItems = JsonConvert.DeserializeObject<YouTubeChannelRankingItem[]>(content);

            var youtubeChannelIds = rankingItems.Select(x => x.ChannelId).ToArray();

            return await youtubeChannelRssCrawler.GetUploadedMovies(youtubeChannelIds, fromUtc, toUtc);
        }

        /// <summary>
        /// </summary>
        /// <returns>PlaylistId</returns>
        public async Task<(string playlistId, string playlistTitle, int videoCount)> GeneratePlaylistFromLatestMoviesJson()
        {
            var fromDateTimeJst = DateTime.UtcNow.AddHours(9).Date.AddDays(-1).AddHours(-9);
            var titleJst = DateTime.UtcNow.AddHours(9).Date.AddDays(-1);
            log.Infomation($"GetOrInsertPlaylist(youTubeService, {fromDateTimeJst})");
            var (playlist, videoCount) = await GetOrInsertPlaylist(youtubeService, titleJst);

            log.Infomation("GeneratePlaylistFromLatestMoviesJson");
            var newMovies = await youtubeBlobService.DownloadLatestYouTubeMovies();
            log.Infomation($"newMovies.Length={newMovies.Length}");

            log.Infomation($"Insert Movies");
            foreach (var movie in newMovies)
            {
                try
                {
                    var insertPlaylistItemRequest = youtubeService.PlaylistItems.Insert(new PlaylistItem
                    {
                        Snippet = new PlaylistItemSnippet
                        {
                            PlaylistId = playlist.Id,
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
                catch (Exception e)
                {
                    log.Error(e.Message, e);
                }
            }

            return (playlist.Id, playlist.Snippet.Title, videoCount);
        }

        private async Task<(Playlist playlist, int videoCount)> GetOrInsertPlaylist(YouTubeService youTubeService, DateTime titleJst)
        {
            var pageToken = default(string);

            var playlistTitle = titleJst.ToString("yyyy年M月dd日") + "のVTuber動画・生放送";

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
                    int playlistItemsCount = await GetPlaylistItemsCount(youTubeService, playlist);

                    return (playlist, playlistItemsCount);
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
                    PrivacyStatus = "public",
                },
            }, "snippet,status");

            insertPlaylistRequest.Fields = "id,snippet/title";

            log.Infomation("insertPlaylistRequest.ExecuteAsync");
            var insertPlaylistResponse = await insertPlaylistRequest.ExecuteAsync();

            return (insertPlaylistResponse, 0);
        }

        private static async Task<int> GetPlaylistItemsCount(YouTubeService youTubeService, Playlist playlist)
        {
            var playlistItemsCount = 0;
            var pageToken = default(string);

            do
            {
                var playlistItemsRequest = youTubeService.PlaylistItems.List("id");
                playlistItemsRequest.PageToken = pageToken;
                playlistItemsRequest.PlaylistId = playlist.Id;
                playlistItemsRequest.MaxResults = 50;

                var playlistItemsResponse = await playlistItemsRequest.ExecuteAsync();

                playlistItemsCount += playlistItemsResponse.Items.Count;

                pageToken = playlistItemsResponse.NextPageToken;

            } while (!string.IsNullOrEmpty(pageToken));
            return playlistItemsCount;
        }
    }
}
