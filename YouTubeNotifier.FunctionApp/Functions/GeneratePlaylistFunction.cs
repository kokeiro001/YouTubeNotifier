using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using YouTubeNotifier.Common;
using YouTubeNotifier.Common.Service;

namespace YouTubeNotifier.FunctionApp
{
    public static class GeneratePlaylistFunction
    {
        [FunctionName("GeneratePlaylistFunction")]
        public static async Task Run([TimerTrigger("0 0 20 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

            var myLogger = new AzureFunctionLogger(log);

            await GeneratePlaylist(myLogger);
        }

        public static async Task GeneratePlaylist(AzureFunctionLogger myLogger)
        {
            var config = Utility.GetConfig();

            var fromUtc = DateTime.UtcNow.AddHours(9).Date.AddDays(-1).AddHours(-9);
            var toUtc = fromUtc.AddDays(1).AddSeconds(-1);

            var serviceConfig = new YouTubeNotifyServiceConfig
            {
                StorageType = StorageType.AzureTableStorage,
                UseCache = false,
                FromDateTimeUtc = fromUtc,
                ToDateTimeUtc = toUtc,
                AzureTableStorageConfig = new AzureTableStorageConfig
                {
                    ConnectionString = config["AzureWebJobsStorage"],
                },
            };

            var youTubeNotifyService = new YouTubeNotifyService(serviceConfig, myLogger);

            await youTubeNotifyService.Run();
        }
    }
}
