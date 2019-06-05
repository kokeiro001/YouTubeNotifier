using System;
using System.Threading.Tasks;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.ConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            var fromUtc = DateTime.UtcNow.AddHours(9).Date.AddDays(-1).AddHours(-9);
            var toUtc = fromUtc.AddDays(1).AddSeconds(-1);

            var config = new YouTubeNotifyServiceConfig
            {
                FromDateTimeUtc = fromUtc,
                ToDateTimeUtc = toUtc,
                UseCache = true,
                StorageType = StorageType.LocalStorage,
                LocalStorageConfig = new LocalStorageConfig
                {
                    ClientSecretFilePath = @"youtubenotifier_client_id.json",
                    CredentialsDirectory = @"Credentials"
                }
            };

            var myLogger = new MyLogger();

            var youtubeNotifyService = new YouTubeNotifyService(config, myLogger);

            await youtubeNotifyService.Run();
        }
    }

    class MyLogger : IMyLogger
    {
        public void Infomation(string message) => Console.WriteLine($"Infomation {message}");

        public void Warning(string message) => Console.WriteLine($"Warning {message}");

        public void Error(string message) => Console.WriteLine($"Error {message}");
    }
}
