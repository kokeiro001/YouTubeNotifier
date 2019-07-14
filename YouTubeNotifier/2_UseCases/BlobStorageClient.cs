using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YouTubeNotifier.UseCases
{
    class BlobStorageClient
    {
        private readonly CloudBlobDirectory blobDirectory;

        public BlobStorageClient(string connectionString, string blobContainerName, string blobDirectoryName)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference(blobContainerName);
            blobContainer.CreateIfNotExistsAsync().Wait();

            blobDirectory = blobContainer.GetDirectoryReference(blobDirectoryName);
        }

        public async Task UploadBlob(string name, Stream stream)
        {
            var blob = blobDirectory.GetBlockBlobReference(name);
            await blob.UploadFromStreamAsync(stream);
        }

        public async Task<CloudBlockBlob[]> ListFiles()
        {
            var token = new BlobContinuationToken();

            var list = new List<CloudBlockBlob>();

            do
            {
                var result = await blobDirectory.ListBlobsSegmentedAsync(token);

                var cloudBlockBlobs = result.Results
                    .Select(x => x as CloudBlockBlob)
                    .Where(x => x != null);

                list.AddRange(cloudBlockBlobs);

                token = result.ContinuationToken;
            }
            while (token != null);

            return list.ToArray();
        }
    }
}
