using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class YouTubeBlobService
    {
        private readonly BlobStorageClient vtuberInsightBlobStroage;
        private readonly BlobStorageClient newMoviesBlobStorage;

        public YouTubeBlobService(string azureStorageConnectionString)
        {
            vtuberInsightBlobStroage = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "VTuberInsight");
            newMoviesBlobStorage = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "NewMovies"); ;
        }

        public async Task UploadVTuberInsightCsvFile(YouTubeChannelRankingItem[] rankingItems)
        {
            var content = JsonConvert.SerializeObject(rankingItems, Formatting.Indented);

            var jst = DateTime.UtcNow.AddHours(9);

            var fileName = jst.ToString("yyyyMMdd_HHmmss") + "_jst_vtuber_insight_ranking.json";

            using (var memoryStream = content.ToMemoryStream())
            {
                await vtuberInsightBlobStroage.UploadBlob(fileName, memoryStream);
            }
        }

        public async Task<string> DownloadLatestVTuberInsightCsvFile()
        {
            var files = await vtuberInsightBlobStroage.ListFiles();

            var data = files.OrderByDescending(x => x.Name);

            var latestFile = data.First();

            return await latestFile.DownloadTextAsync();
        }

        public async Task UploadLatestYouTubeMovies(DateTime fromUtc, DateTime toUtc, YouTubeRssItem[] youtubeRssItems)
        {
            var fromJst = fromUtc.AddHours(9);
            var toJst = toUtc.AddHours(9);

            var fromStr = fromJst.ToString("yyyyMMdd_HHmmss") + "_jst";
            var toStr = toJst.ToString("yyyyMMdd_HHmmss") + "_jst";

            var fileName = $"{fromStr}-{toStr}_new_movies.json";

            var content = JsonConvert.SerializeObject(youtubeRssItems, Formatting.Indented);

            using (var memoryStream = content.ToMemoryStream())
            {
                await newMoviesBlobStorage.UploadBlob(fileName, memoryStream);
            }
        }

        public async Task<YouTubeRssItem[]> DownloadLatestYouTubeVideos()
        {
            var files = await newMoviesBlobStorage.ListFiles();

            var data = files.OrderByDescending(x => x.Name);

            var latestFile = data.First();

            var text = await latestFile.DownloadTextAsync();

            return JsonConvert.DeserializeObject<YouTubeRssItem[]>(text);
        }
    }
}
