using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task UploadLatestYouTubeMovies(YouTubeRssItem[] youtubeRssItems)
        {
            var jst = DateTime.UtcNow.AddHours(9);

            var fileName = jst.ToString("yyyyMMdd_HHmmss") + "_jst_new_movies.json";

            var content = JsonConvert.SerializeObject(youtubeRssItems, Formatting.Indented);
            var blobStorageClient = new BlobStorageClient(azureStorageConnectionString, "vtuberranking", "NewMovies");

            using (var memoryStream = content.ToMemoryStream())
            {
                await blobStorageClient.UploadBlob(fileName, memoryStream);
            }
        }
    }
}
