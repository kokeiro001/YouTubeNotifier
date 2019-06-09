using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YouTubeNotifier.Common.Repository;

namespace YouTubeNotifier.Common.Service
{
    static class YoutubeServiceCreator
    {
        private static readonly string[] Scopes = { YouTubeService.Scope.Youtube };
        private static readonly string ApplicationName = "YouTubeNotifier";

        public static async Task<YouTubeService> Create(YouTubeNotifyServiceConfig config)
        {
            UserCredential credential;

            if (config.StorageType == StorageType.LocalStorage)
            {
                credential = await GetCredentialFromLocalFile(config);
            }
            else if (config.StorageType == StorageType.AzureTableStorage)
            {
                credential = await GetCredentialFromTableStoraeg(config);
            }
            else
            {
                throw new Exception($"unexpedted type config.StorageType={config.StorageType}");
            }

            return new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private static Task<UserCredential> GetCredentialFromLocalFile(YouTubeNotifyServiceConfig config)
        {
            var clientSecretFilePath = config.LocalStorageConfig.ClientSecretFilePath;

            using (var stream = new FileStream(clientSecretFilePath, FileMode.Open, FileAccess.Read))
            {
                var dataStore = new FileDataStore(config.LocalStorageConfig.CredentialsDirectory, true);

                return GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    dataStore
                );
            }
        }

        private static async Task<UserCredential> GetCredentialFromTableStoraeg(YouTubeNotifyServiceConfig config)
        {
            var cloudStorageAccountConnectionString = config.AzureTableStorageConfig.ConnectionString;

            var secretStore = new TableStorageSecretStore(cloudStorageAccountConnectionString);
            var secretText = await secretStore.GetSecret();

            using (var stream = secretText.ToMemoryStream(Encoding.UTF8))
            {
                var dataStore = new TableStorageDataStore(cloudStorageAccountConnectionString);

                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    dataStore
                );
            }
        }
    }
}
