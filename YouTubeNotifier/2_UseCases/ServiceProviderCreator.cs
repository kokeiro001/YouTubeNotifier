using Google.Apis.YouTube.v3;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using YouTubeNotifier.Common;
using YouTubeNotifier.Entities;

namespace YouTubeNotifier.UseCases
{
    static class ServiceProviderCreator
    {
        public static async Task<IServiceProvider> Create()
        {
            var serviceCollection = new ServiceCollection();

            var settingsJson = File.ReadAllText(@"settings.json");
            var settings = JsonConvert.DeserializeObject<Settings>(settingsJson);

            serviceCollection.AddSingleton<Log4NetLogger>(Log4NetLogger.Create());

            serviceCollection.AddSingleton<Settings>(settings);

            serviceCollection.AddSingleton<VTuberRankingService>();

            serviceCollection.AddSingleton<TwitterService>(new TwitterService(settings.Twitter));

            serviceCollection.AddSingleton<YouTubeBlobService>(new YouTubeBlobService(settings.AzureCloudStorageConnectionString));

            serviceCollection.AddSingleton<VTuberInsightCrawler>();

            serviceCollection.AddSingleton<YouTubeChannelRssCrawler>();

            var youttubeService = await YoutubeServiceCreator.Create(settings.AzureCloudStorageConnectionString);

            serviceCollection.AddSingleton<YouTubeService>(youttubeService);

            return serviceCollection.BuildServiceProvider();
        }
    }
}
