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

            var myLogger = new MyLogger(log);

            await GeneratePlaylist(myLogger);
        }

        public static async Task GeneratePlaylist(MyLogger myLogger)
        {
            var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(executingPath))
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

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

    // なんかいいnuget探してきたほうがいい
    public class MyLogger : IMyLogger
    {
        private readonly ILogger log;

        public MyLogger(ILogger log)
        {
            this.log = log;
        }

        public void Infomation(string message) => log.LogInformation(message);

        public void Warning(string message) => log.LogWarning(message);

        public void Error(string message) => log.LogError(message);
    }
}
