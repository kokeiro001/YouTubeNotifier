using Microsoft.Extensions.Logging;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.FunctionApp
{
    public class AzureFunctionLogger : IMyLogger
    {
        private readonly ILogger log;

        public AzureFunctionLogger(ILogger log)
        {
            this.log = log;
        }

        public void Infomation(string message) => log.LogInformation(message);

        public void Warning(string message) => log.LogWarning(message);

        public void Error(string message) => log.LogError(message);
    }
}
