using System.Threading.Tasks;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.ConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            var config = new YouTubeNotifyServiceConfig
            {
                UseCache = true,
                StorageType = StorageType.LocalStorage,
                LocalStorageConfig = new LocalStorageConfig
                {
                    ClientSecretFilePath = @"youtubenotifier_client_id.json",
                    CredentialsDirectory = @"Credentials"
                }
            };

            var youtubeNotifyService = new YouTubeNotifyService(config);
            await youtubeNotifyService.Run();
        }
    }

}
