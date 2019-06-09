using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace YouTubeNotifier.FunctionApp
{
    public static class Utility
    {
        public static IConfigurationRoot GetConfig()
        {
            var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(executingPath))
                .AddJsonFile("local.settings.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            return config;
        }
    }
}
