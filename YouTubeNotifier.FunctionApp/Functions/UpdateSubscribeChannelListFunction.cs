using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YouTubeNotifier.Common;
using YouTubeNotifier.Common.Service;

namespace YouTubeNotifier.FunctionApp
{
    public static class UpdateSubscribeChannelListFunction
    {
        [FunctionName("UpdateSubscribeChannelListFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

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

            var logger = new AzureFunctionLogger(log);

            var youTubeNotifyService = new YouTubeNotifyService(serviceConfig, logger);

            await youTubeNotifyService.UpdateSubscriptionChannelList("MyAccountSubscription");

            return new OkObjectResult("success");
        }
    }
}
