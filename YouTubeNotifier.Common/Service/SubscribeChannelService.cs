using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YouTubeNotifier.Common.Repository;

namespace YouTubeNotifier.Common.Service
{
    public class SubscribeChannelService
    {
        private readonly YouTubeNotifyServiceConfig config;
        private readonly IMyLogger log;
        private YouTubeService youTubeService;

        public SubscribeChannelService(YouTubeNotifyServiceConfig config, IMyLogger log)
        {
            this.config = config;
            this.log = log;
        }

        public async Task UpdateSubscriptionChannelList(string categoryName)
        {
            var repository = new SubscriptionChannelRepository(config.AzureTableStorageConfig.ConnectionString);

            youTubeService = await YoutubeServiceCreator.Create(config);

            var channelList = await GetSubscriptionYouTubeChannels(true);

            foreach (var channel in channelList)
            {
                await repository.AddOrInsert(categoryName, channel.Snippet.ResourceId.ChannelId, channel.Snippet.Title);
            }
        }

        private async Task<List<Subscription>> GetSubscriptionYouTubeChannels(bool getTitle)
        {
            var list = new List<Subscription>();
            var pageToken = default(string);

            do
            {
                var subscriptionsListRequest = youTubeService.Subscriptions.List("snippet");
                subscriptionsListRequest.Fields = "nextPageToken,items/snippet/resourceId/channelId";
                subscriptionsListRequest.Mine = true;
                subscriptionsListRequest.MaxResults = 50;
                subscriptionsListRequest.PageToken = pageToken;

                if (getTitle)
                {
                    subscriptionsListRequest.Fields += ",items/snippet/title";
                }

                log.Infomation("subscriptionsListRequest.ExecuteAsync");
                var subscriptionList = await subscriptionsListRequest.ExecuteAsync();

                foreach (var subscription in subscriptionList.Items)
                {
                    list.Add(subscription);
                }

                pageToken = subscriptionList.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return list;
        }

        // TODO: AzureFunctionsでcsv入力はやばい。ローカルで動かす限定の作りになってる。
        // コンソールアプリに移動する。
        public async Task UpdateSubscriptionChannelListByCsv(string categoryName, string csvFilePath)
        {
            var repository = new SubscriptionChannelRepository(config.AzureTableStorageConfig.ConnectionString);

            youTubeService = await YoutubeServiceCreator.Create(config);

            var channelIds = File.ReadAllLines(csvFilePath)
                .Skip(1)
                .Take(100)
                .Select(x => x.Split(',')[2].Trim('"', ' '))
                .Select(x => x.Replace("https://www.youtube.com/channel/", ""));

            foreach (var channelId in channelIds)
            {
                await repository.AddOrInsert(categoryName, channelId, null);
            }
        }
    }
}
