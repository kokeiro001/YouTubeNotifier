using System.IO;
using System.Text;

namespace YouTubeNotifier.VTuberRankingCrawler.Common
{
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
