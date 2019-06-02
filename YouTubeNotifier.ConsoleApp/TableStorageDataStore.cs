using Google.Apis.Util.Store;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace YouTubeNotifier.ConsoleApp
{
    // https://gist.github.com/NoelOConnell/0dee5fc144b0d56a2ce0aa0411316de9
    public class DataStoreItem : TableEntity
    {
        public string Value { get; set; }
    }

    public class TableStorageDataStore : IDataStore
    {
        private CloudTable _table;
        public const string TABLE_NAME = "GoogleDataStore";
        public const string PARTITION_NAME = "OAuth2Responses";

        public TableStorageDataStore(string cloudStorageAccountConnectionString)
        {
            // Get TableStorage Connection String
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(cloudStorageAccountConnectionString);

            var tableClient = storageAccount.CreateCloudTableClient();
            _table = tableClient.GetTableReference(TABLE_NAME);
            _table.CreateIfNotExistsAsync().Wait();
        }

        public async Task ClearAsync()
        {
            // A batch operation may contain up to 100 individual table operations
            // https://docs.microsoft.com/en-us/dotnet/api/microsoft.windowsazure.storage.table.tablebatchoperation?view=azure-dotnet

            TableQuery<DataStoreItem> query = new TableQuery<DataStoreItem>();
            var tableContinuationToken = new TableContinuationToken();
            var items = await _table.ExecuteQuerySegmentedAsync(query, tableContinuationToken);

            int chunkSize = 99;

            var batchedItems = items.Select((x, i) => new { Index = i, Value = x })
                 .GroupBy(x => x.Index / chunkSize)
                 .Select(x => x.Select(v => v.Value).ToList())
                 .ToList();

            foreach (List<DataStoreItem> batch in batchedItems)
            {
                TableBatchOperation batchDeleteOperation = new TableBatchOperation();

                foreach (DataStoreItem item in batch)
                    batchDeleteOperation.Delete(item);

                await _table.ExecuteBatchAsync(batchDeleteOperation);
            }

            await Task.CompletedTask;
        }

        public async Task DeleteAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            DataStoreItem item = await GetAsync<DataStoreItem>(key);

            var deleteOperation = TableOperation.Delete(item);
            await _table.ExecuteAsync(deleteOperation);
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            string generatedStoreKey = GenerateStoredKey(key, typeof(T));

            TableOperation retrieveOperation = TableOperation.Retrieve<DataStoreItem>(PARTITION_NAME, generatedStoreKey);
            TableResult retrievedResult = await _table.ExecuteAsync(retrieveOperation);

            DataStoreItem item = (DataStoreItem)retrievedResult.Result;

            T value = item == null ? default(T) : JsonConvert.DeserializeObject<T>(item.Value);

            return value;
        }

        public async Task StoreAsync<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }

            string serializedValue = JsonConvert.SerializeObject(value);
            string generatedStoreKey = GenerateStoredKey(key, typeof(T));

            DataStoreItem item = new DataStoreItem() { PartitionKey = PARTITION_NAME, RowKey = generatedStoreKey, Value = serializedValue };

            TableOperation insertOperation = TableOperation.InsertOrMerge(item);
            TableResult result = await _table.ExecuteAsync(insertOperation);

            if (result.HttpStatusCode < (int)HttpStatusCode.OK || result.HttpStatusCode > (int)HttpStatusCode.MultipleChoices)
            {
                throw new Exception($"[{result.HttpStatusCode}] Failed to insert record at {TABLE_NAME} {item.RowKey}");
            }

            await Task.CompletedTask;
        }

        // <summary>Creates a unique stored key based on the key and the class type.</summary>
        /// <param name="key">The object key.</param>
        /// <param name="t">The type to store or retrieve.</param>
        public static string GenerateStoredKey(string key, Type t)
        {
            return $"{t.FullName}-{key}";
        }
    }
}
