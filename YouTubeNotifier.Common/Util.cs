using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace YouTubeNotifier.Common
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

    public static class ConverterExtensions
    {
        /// <summary>
        /// 文字エンコーディングを指定して現在の文字列を表すメモリストリームを取得します。
        /// </summary>
        /// <param name="text">現在の文字列</param>
        /// <param name="encoding">文字エンコーディング</param>
        /// <returns>メモリストリーム</returns>
        public static MemoryStream ToMemoryStream(this string text, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(text));
        }

        /// <summary>
        /// 文字エンコーディングを指定して現在のメモリストリームを表す文字列を取得します。
        /// </summary>
        /// <param name="stream">メモリストリーム</param>
        /// <param name="encoding">文字エンコーディング</param>
        /// <returns>文字列</returns>
        public static string ToString(this MemoryStream stream, Encoding encoding)
        {
            return encoding.GetString(stream.ToArray());
        }
    }
}
