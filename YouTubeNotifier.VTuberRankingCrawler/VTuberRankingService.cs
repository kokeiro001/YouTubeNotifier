using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class VTuberRankingService
    {
        private readonly Settings settings;
        private readonly YouTubeBlobService youtubeBlobService;

        public VTuberRankingService(Settings settings)
        {
            this.settings = settings;
            youtubeBlobService = new YouTubeBlobService(settings.AzureCloudStorageConnectionString);
        }

        public async Task Run()
        {
            var vtuberInsightCrawler = new VTuberInsightCrawler();

            var rankingItems = await vtuberInsightCrawler.Run();

            foreach (var rankingItem in rankingItems)
            {
                Console.WriteLine($"{rankingItem.Rank:D3} {rankingItem.ChannelName} {rankingItem.ChannelId}");
            }

            await youtubeBlobService.UploadVTuberInsightCsvFile(rankingItems);

            var fromUtc = DateTime.UtcNow.AddHours(9).Date.AddDays(-2).AddHours(-9);
            var toUtc = fromUtc.AddDays(1);

            var latestYouYubeRssItems = await GetMovieIds(fromUtc, toUtc);

            await youtubeBlobService.UploadLatestYouTubeMovies(latestYouYubeRssItems);
        }

        private async Task<YouTubeRssItem[]> GetMovieIds(DateTime fromUtc, DateTime toUtc)
        {
            var content = await youtubeBlobService.DownloadLatestVTuberInsightCsvFile();
            var rankingItems = JsonConvert.DeserializeObject<YouTubeChannelRankingItem[]>(content);

            var youtubeChannelRssCrawler = new YouTubeChannelRssCrawler();
            var youtubeChannelIds = rankingItems.Select(x => x.ChannelId).ToArray();

            return await youtubeChannelRssCrawler.GetUploadedMovies(youtubeChannelIds, fromUtc, toUtc);
        }
    }
}
