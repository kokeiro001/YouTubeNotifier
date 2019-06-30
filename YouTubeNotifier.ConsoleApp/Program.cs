using System;
using System.Threading.Tasks;
using YouTubeNotifier.Common;
using YouTubeNotifier.Common.Service;

namespace YouTubeNotifier.ConsoleApp
{
    class Program
    {
        static async Task Main()
        {
            var fromUtc = DateTime.UtcNow.AddHours(9).Date.AddDays(-1).AddHours(-9);
            var toUtc = fromUtc.AddDays(1).AddSeconds(-1);

            //var myLogger = new ConsoleLogger();

            Console.WriteLine($"fromUtc={fromUtc}");
            Console.WriteLine($"toUtc={toUtc}");
        }
    }
}
