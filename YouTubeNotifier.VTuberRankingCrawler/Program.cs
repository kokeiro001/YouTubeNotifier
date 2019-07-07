using Newtonsoft.Json;
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

            var vtuberRankingService = new VTuberRankingService(settings);
            await vtuberRankingService.GetNewMovies();

            await vtuberRankingService.GeneratePlaylistFromLatestMoviesJson();
        }
    }
}
