using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace YouTubeNotifier.ConsoleApp
{
    public static class Util
    {
        private static readonly string CacheDirectoryName = "Cache";

        public static T Cache<T>(Func<T> dataFecher, string key)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var cacheDirectory = Path.Combine(currentDirectory, CacheDirectoryName);

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            var filePath = Path.Combine(cacheDirectory, $"{key}.json");

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            else
            {
                var data = dataFecher();

                var json = JsonConvert.SerializeObject(data);
                File.WriteAllText(filePath, json);

                return data;
            }
        }

        public static async Task<T> Cache<T>(Func<Task<T>> dataFecher, string key)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var cacheDirectory = Path.Combine(currentDirectory, CacheDirectoryName);

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }

            var filePath = Path.Combine(cacheDirectory, $"{key}.json");

            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            else
            {
                var data = await dataFecher();

                var json = JsonConvert.SerializeObject(data);
                File.WriteAllText(filePath, json);

                return data;
            }
        }
    }
}
