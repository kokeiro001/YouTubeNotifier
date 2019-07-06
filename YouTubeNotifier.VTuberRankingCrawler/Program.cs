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

            var vtuberRankingService = new VTuberRankingService(settings);
            await vtuberRankingService.GetNewMovies();

            await vtuberRankingService.GeneratePlaylistFromLatestMoviesJson();
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
