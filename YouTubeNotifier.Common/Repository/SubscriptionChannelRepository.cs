using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace YouTubeNotifier.Common.Repository
{
    public class SubscriptionChannelInfo : TableEntity
    {
        public string YouTubeChannelId { get; set; }

        public string Title { get; set; }

        public string CategoryName { get; set; }

        public void SetupTableStorageInfo()
        {
            PartitionKey = CategoryName;
            RowKey = YouTubeChannelId;
        }
    }

    class SubscriptionChannelRepository
    {
        public static readonly string TableName = "SubscriptionChannels";

        private readonly CloudTable cloudTable;

        public SubscriptionChannelRepository(string cloudStorageAccountConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(cloudStorageAccountConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            cloudTable = tableClient.GetTableReference(TableName);
            cloudTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task AddOrInsert(string categoryName, string channelId, string title)
        {
            var subscriptionChannelInfo = new SubscriptionChannelInfo
            {
                CategoryName = categoryName,
                YouTubeChannelId = channelId,
                Title = title,
            };

            subscriptionChannelInfo.SetupTableStorageInfo();

            var insertOperation = TableOperation.InsertOrMerge(subscriptionChannelInfo);

            await cloudTable.ExecuteAsync(insertOperation);
        }
    }
}
