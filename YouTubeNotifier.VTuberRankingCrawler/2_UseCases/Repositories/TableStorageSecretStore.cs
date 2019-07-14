using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler.UseCases.Repositories
{
    public class TableStorageSecretStore
    {
        public const string TABLE_NAME = "GoogleSecretDataStore";
        public const string PARTITION_NAME = "GoogleSecretData";

        private readonly CloudTable cloudTable;

        public TableStorageSecretStore(string cloudStorageAccountConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(cloudStorageAccountConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            cloudTable = tableClient.GetTableReference(TABLE_NAME);
            cloudTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task<string> GetSecret()
        {
            var retrieveOperation = TableOperation.Retrieve<DataStoreItem>(PARTITION_NAME, "0");
            var retrievedResult = await cloudTable.ExecuteAsync(retrieveOperation);

            DataStoreItem item = (DataStoreItem)retrievedResult.Result;

            return item.Value;
        }
    }
}
