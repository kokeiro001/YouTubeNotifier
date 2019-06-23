using AngleSharp.Html.Parser;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class YouTubeChannelRankingItem
    {
        public int Rank { get; set; }

        public string ChannelName { get; set; }

        public string ChannelId { get; set; }
    }

    class VTuberInsightCrawler
    {
        public async Task<YouTubeChannelRankingItem[]> Run()
        {
            var retryCount = 0;
            var maxRetryCount = 3;

            do
            {
                try
                {
                    retryCount++;

                    var html = await GetPageSource();

                    var rankingItems = Parse(html).ToArray();

                    if (rankingItems.Length > 0)
                    {
                        return rankingItems;
                    }
                }
                catch
                {
                    if (retryCount >= maxRetryCount)
                    {
                        throw;
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
            while (retryCount < maxRetryCount);

            throw new Exception("not found element");
        }

        private IEnumerable<YouTubeChannelRankingItem> Parse(string html)
        {
            var htmlDocument = new HtmlParser().ParseDocument(html);

            var workspace = htmlDocument.QuerySelectorAll("div")
                .Where(x => x.Id == "workspace")
                .First();

            var table = workspace.QuerySelectorAll("table")
                .Where(x => x.GetAttribute("border") == "1")
                .First();

            var items = new List<YouTubeChannelRankingItem>();

            foreach (var tr in table.QuerySelectorAll("tr").Skip(2))
            {
                var tds = tr.QuerySelectorAll("td").ToArray();

                items.Add(new YouTubeChannelRankingItem
                {
                    Rank = int.Parse(tds[1].TextContent),
                    ChannelName = tds[3].TextContent,
                    ChannelId = tds[3].QuerySelector("a").Id,
                });
            }

            return items
                .Where(x => !string.IsNullOrEmpty(x.ChannelName));
        }

        private static async Task<string> GetPageSource()
        {
            var driverPath = "/opt/selenium/";
            var driverExecutableFileName = "chromedriver";

            var options = new ChromeOptions();
            options.AddArguments("headless");
            options.AddArguments("no-sandbox");
            options.BinaryLocation = "/opt/google/chrome/chrome";
            using (var service = ChromeDriverService.CreateDefaultService(driverPath, driverExecutableFileName))
            using (var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(30)))
            {
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(40);
                driver.Manage().Window.Maximize();

                var url = @"https://vtuber-insight.com/index.html";

                driver.Navigate().GoToUrl(url);

                await Task.Delay(TimeSpan.FromSeconds(20));

                return driver.PageSource;
            }
        }
    }
}
