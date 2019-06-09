using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace YouTubeNotifier.FunctionApp.Test
{
    public static class TestGeneratePlaylistFunction
    {
        [FunctionName("TestGeneratePlaylistFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var myLogger = new AzureFunctionLogger(log);

            await GeneratePlaylistFunction.GeneratePlaylist(myLogger);

            return new OkResult();
        }
    }
}
