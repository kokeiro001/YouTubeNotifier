using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YouTubeNotifier.VTuberRankingCrawler.Common;
using YouTubeNotifier.VTuberRankingCrawler.UseCases.Repositories;

namespace YouTubeNotifier.Common.Service
{
    public static class YoutubeServiceCreator
    {
        private static readonly string[] Scopes = { YouTubeService.Scope.Youtube };
        private static readonly string ApplicationName = "YouTubeNotifier";

        public static async Task<YouTubeService> Create(string azureTableStorageConnectionString)
        {
            var credential = await GetCredentialFromTableStoraeg(azureTableStorageConnectionString);

            return new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        private static async Task<UserCredential> GetCredentialFromTableStoraeg(string azureTableStorageConnectionString)
        {
            var cloudStorageAccountConnectionString = azureTableStorageConnectionString;

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
