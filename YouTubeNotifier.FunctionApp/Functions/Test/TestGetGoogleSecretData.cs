using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using YouTubeNotifier.Common.Repository;
using System.Text;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using YouTubeNotifier.Common;
using Google.Apis.YouTube.v3;

namespace YouTubeNotifier.FunctionApp.Functions.Test
{
    public static class TestGetGoogleSecretData
    {
        private static readonly string[] Scopes = { YouTubeService.Scope.Youtube };
        private static readonly string ApplicationName = "YouTubeNotifier";


        [FunctionName("TestGetGoogleSecretData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("TestGetGoogleSecretData trigger function processed a request.");

            var config = Utility.GetConfig();

            var connectionString = config["AzureWebJobsStorage"];

            await GetCredentialFromTableStoraeg(connectionString);

            return new OkResult();
        }

        private static async Task<UserCredential> GetCredentialFromTableStoraeg(string cloudStorageAccountConnectionString)
        {
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
