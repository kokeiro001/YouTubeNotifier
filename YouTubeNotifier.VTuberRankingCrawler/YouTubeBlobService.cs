using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class YouTubeBlobService
    {
        private readonly string azureStorageConnectionString;

        public YouTubeBlobService(string azureStorageConnectionString)
        {
            this.azureStorageConnectionString = azureStorageConnectionString;
        }

        public async Task UploadVTuberInsightCsvFile(YouTubeChannelRankingItem[] rankingItems)
        {
            var blobStorageClient = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "VTuberInsight");

            var content = JsonConvert.SerializeObject(rankingItems, Formatting.Indented);

            var jst = DateTime.UtcNow.AddHours(9);

            var fileName = jst.ToString("yyyyMMdd_HHmmss") + "_jst_vtuber_insight_ranking.json";

            using (var memoryStream = content.ToMemoryStream())
            {
                await blobStorageClient.UploadBlob(fileName, memoryStream);
            }
        }

        public async Task<string> DownloadLatestVTuberInsightCsvFile()
        {
            var blobStorageClient = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "VTuberInsight");

            var files = await blobStorageClient.ListFiles();

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
            var blobStorageClient = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "NewMovies");

            using (var memoryStream = content.ToMemoryStream())
            {
                await blobStorageClient.UploadBlob(fileName, memoryStream);
            }
        }

        public async Task<YouTubeRssItem[]> DownloadLatestYouTubeMovies()
        {
            var blobStorageClient = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "NewMovies");

            var files = await blobStorageClient.ListFiles();

            var data = files.OrderByDescending(x => x.Name);

            var latestFile = data.First();

            var text = await latestFile.DownloadTextAsync();

            return JsonConvert.DeserializeObject<YouTubeRssItem[]>(text);
        }
    }
}
