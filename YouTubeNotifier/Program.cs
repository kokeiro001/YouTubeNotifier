using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using YouTubeNotifier.UseCases;

namespace YouTubeNotifier
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
            var serviceProvider = await ServiceProviderCreator.Create();
            
            var vtuberRankingService = serviceProvider.GetService<VTuberRankingService>();

            await vtuberRankingService.GetNewMovies();

            var (playlistId, playlistTitle, videoCount) = await vtuberRankingService.GeneratePlaylistFromLatestMoviesJson();

            var twitterService = serviceProvider.GetService<TwitterService>();

            await twitterService.TweetGeneratedPlaylist(playlistId, playlistTitle, videoCount);
        }
    }
}
