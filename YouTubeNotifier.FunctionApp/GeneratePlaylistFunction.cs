using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.FunctionApp
{
    public static class GeneratePlaylistFunction
    {
        [FunctionName("GeneratePlaylistFunction")]
        public static async Task Run([TimerTrigger("0 0 20 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

            var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(executingPath))
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var serviceConfig = new YouTubeNotifyServiceConfig
            {
                UseCache = false,
                AzureTableStorageConfig = new AzureTableStorageConfig
                {
                    ConnectionString = config["AzureWebJobsStorage"],
                },
            };

            var youTubeNotifyService = new YouTubeNotifyService(serviceConfig);
            await youTubeNotifyService.Run();
        }
    }
}
