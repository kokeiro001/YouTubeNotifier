using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using YouTubeNotifier.Common.Repository;

namespace YouTubeNotifier.Common.Service
{
    public class UpdateSubscribeChannelListService
    {
        private readonly YouTubeNotifyServiceConfig config;
        private readonly IMyLogger log;
        private YouTubeService youTubeService;

        public UpdateSubscribeChannelListService(YouTubeNotifyServiceConfig config, IMyLogger log)
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
                await repository.AddOrInsert(categoryName, channel.Snippet.ChannelId, channel.Snippet.Title);
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
    }
}
