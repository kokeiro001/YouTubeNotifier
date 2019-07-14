using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler.Common
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

        public static string JoinString(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source.ToArray());
        }
    }

    public static class ConverterExtensions
    {
        public static MemoryStream ToMemoryStream(this string text)
        {
            return ToMemoryStream(text, Encoding.UTF8);
        }

        public static MemoryStream ToMemoryStream(this string text, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(text));
        }

        public static string ToStringWithEncoding(this MemoryStream stream, Encoding encoding)
        {
            return encoding.GetString(stream.ToArray());
        }
    }
}
