using System;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                var vtuberInsightCrawler = new VTuberInsightCrawler();

                var rankingItems = await vtuberInsightCrawler.Run();

                foreach (var rankingItem in rankingItems)
                {
                    Console.WriteLine($"{rankingItem.Rank:D3} {rankingItem.ChannelName}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} {e.StackTrace}");
            }
        }
    }
}
