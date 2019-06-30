using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YouTubeNotifier.Common;
using YouTubeNotifier.Common.Service;

namespace YouTubeNotifier.FunctionApp.Functions
{
    // TODO: fucntion move to console application.
    public static class UpdateRankingSubscribeChannelListFunction
    {
        [FunctionName("UpdateRankingSubscribeChannelListFunction")]
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
                UseCache = false,
                FromDateTimeUtc = fromUtc,
                ToDateTimeUtc = toUtc,
                AzureTableStorageConnectionString= config["AzureWebJobsStorage"],
            };

            var csvFilePath = @"C:\Users\kokei\Downloads\VtuberInsight_export_subscriberCount_2019_6_11_1_5.csv";

            var logger = new AzureFunctionLogger(log);

            var subscribeChannelService = new SubscribeChannelService(serviceConfig, logger);

            await subscribeChannelService.UpdateSubscriptionChannelListByCsv("FavoriteVTubers", csvFilePath);

            return new OkObjectResult("success");
        }
    }
}
