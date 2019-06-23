using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
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
    }
}
