using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    public class Settings
    {
        public string AzureCloudStorageConnectionString { get; set; }

        public TwitterSettings Twitter { get; set; }

        public class TwitterSettings
        {
            public string ApiKey { get; set; }
            public string ApiSecret { get; set; }
            public string AccessToken { get; set; }
            public string AccessTokenSecret { get; set; }
        }
    }

    class Program
    {
        static async Task Main()
        {
            var settingsJson = File.ReadAllText(@"settings.json");
            var settings = JsonConvert.DeserializeObject<Settings>(settingsJson);

            var vtuberRankingService = new VTuberRankingService(settings);

            await vtuberRankingService.GetNewMovies();

            var (playlistId, playlistTitle, videoCount) = await vtuberRankingService.GeneratePlaylistFromLatestMoviesJson();

            var twitterService = new TwitterService(settings);

            await twitterService.TweetGeneratedPlaylist(playlistId, playlistTitle, videoCount);
        }
    }
}
