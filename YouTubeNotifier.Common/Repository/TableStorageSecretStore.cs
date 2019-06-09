using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;

namespace YouTubeNotifier.Common.Repository
{
    public class TableStorageSecretStore
    {
        private CloudTable _table;
        public const string TABLE_NAME = "GoogleSecretDataStore";
        public const string PARTITION_NAME = "GoogleSecretData";

        public TableStorageSecretStore(string cloudStorageAccountConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(cloudStorageAccountConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(TABLE_NAME);
            _table.CreateIfNotExistsAsync().Wait();
        }

        public async Task<string> GetSecret()
        {
            var retrieveOperation = TableOperation.Retrieve<DataStoreItem>(PARTITION_NAME, "0");
            var retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            DataStoreItem item = (DataStoreItem)retrievedResult.Result;

            return item.Value;
        }
    }
}
