using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    public class Settings
    {
        public string AzureCloudStorageConnectionString { get; set; }
    }

    class Program
    {
        static async Task Main()
        {
            var settingsJson = File.ReadAllText(@"settings.json");
            var settings = JsonConvert.DeserializeObject<Settings>(settingsJson);

            //var vtuberInsightCrawler = new VTuberInsightCrawler();

            //var rankingItems = await vtuberInsightCrawler.Run();

            //foreach (var rankingItem in rankingItems)
            //{
            //    Console.WriteLine($"{rankingItem.Rank:D3} {rankingItem.ChannelName} {rankingItem.ChannelId}");
            //}

            //await UploadRankingCsvFile(settings.AzureCloudStorageConnectionString, rankingItems);

            var content = await DownloadRankingCsvFile(settings.AzureCloudStorageConnectionString);
            var rankingItems = JsonConvert.DeserializeObject<YouTubeChannelRankingItem[]>(content);

            var youtubeChannelRssCrawler = new YouTubeChannelRssCrawler();
            var youtubeChannelIds = rankingItems.Select(x => x.ChannelId).ToArray();

            var fromUtc = DateTime.UtcNow.Date.AddDays(-1);
            var toUtc = fromUtc.AddDays(1);

            var newMovieIds = await youtubeChannelRssCrawler.GetUploadedMovieIds(youtubeChannelIds, fromUtc, toUtc);
        }

        private static async Task UploadRankingCsvFile(string connectionString, YouTubeChannelRankingItem[] rankingItems)
        {
            var blobStorageClient = new BlobStorageClient(connectionString, "vtuberranking", "VTuberInsight");

            var content = JsonConvert.SerializeObject(rankingItems);

            var jst = DateTime.UtcNow.AddHours(9);

            var fileName = jst.ToString("yyyyMMdd_HHmmss") + "_jst.json";

            using (var memoryStream = content.ToMemoryStream())
            {
                await blobStorageClient.UploadBlob(fileName, memoryStream);
            }
        }

        private static async Task<string> DownloadRankingCsvFile(string connectionString)
        {
            var blobStorageClient = new BlobStorageClient(connectionString, "vtuberranking", "VTuberInsight");

            var files = await blobStorageClient.ListFiles();

            var data = files.OrderByDescending(x => x.Name);

            var latestFile = data.First();

            return await latestFile.DownloadTextAsync();
        }
    }

    public static class UtilityExtensions
    {
        public static MemoryStream ToMemoryStream(this string text)
        {
            return ToMemoryStream(text, Encoding.UTF8);
        }

        public static MemoryStream ToMemoryStream(this string text, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(text));
        }

        public static string ToStringWithEncoding(this MemoryStream stream, Encoding encoding)
        {
            return encoding.GetString(stream.ToArray());
        }
    }
}
