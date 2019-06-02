using System.Threading.Tasks;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.ConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            var youtubeNotifyService = new YouTubeNotifyService();
            await youtubeNotifyService.Run();
        }
    }

}
