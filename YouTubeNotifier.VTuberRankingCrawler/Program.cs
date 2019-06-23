using Newtonsoft.Json;
using System;
using System.IO;
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

            var vtuberInsightCrawler = new VTuberInsightCrawler();

            var rankingItems = await vtuberInsightCrawler.Run();

            foreach (var rankingItem in rankingItems)
            {
                Console.WriteLine($"{rankingItem.Rank:D3} {rankingItem.ChannelName} {rankingItem.ChannelId}");
            }

            await UploadFile(settings.AzureCloudStorageConnectionString, rankingItems);
        }

        private static async Task UploadFile(string connectionString, YouTubeChannelRankingItem[] rankingItems)
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
    }
}
