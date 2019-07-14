using AngleSharp.Html.Parser;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YouTubeNotifier.Common;

namespace YouTubeNotifier.Entities
{
    class YouTubeChannelRankingItem
    {
        public int Rank { get; set; }

        public string ChannelName { get; set; }

        public string ChannelId { get; set; }
    }

    class VTuberInsightCrawler
    {
        private readonly Log4NetLogger log;

        public VTuberInsightCrawler(Log4NetLogger log)
        {
            this.log = log;
        }

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

                    log.Infomation("** Begin Parse");

                    var rankingItems = Parse(html).ToArray();

                    log.Infomation($"** rankingItems.Length={rankingItems.Length}");

                    if (rankingItems.Length > 0)
                    {
                        return rankingItems;
                    }
                }
                catch (Exception e)
                {
                    log.Error($"** e.Message={e.Message}");
                    log.Error($"** e.StackTrace={e.StackTrace}");
                    if (retryCount >= maxRetryCount)
                    {
                        throw;
                    }
                }

                log.Error($"** failed GetRankingItems. retryCount={retryCount}");
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
                    ChannelId = tds[3].QuerySelector("a")?.Id,
                });
            }

            return items
                .Where(x => !string.IsNullOrEmpty(x.ChannelName));
        }

        private async Task<string> GetPageSource()
        {
            var driverPath = "/opt/selenium/";
            var driverExecutableFileName = "chromedriver";

            var options = new ChromeOptions();
            options.AddArguments("headless");
            options.AddArguments("--disable-gpu");
            options.AddArguments("no-sandbox");
            options.AddArguments("--window-size=1,1");
            options.AddArguments("--disable-desktop-notifications");
            options.AddArguments("--disable-extensions");
            options.AddArguments("--blink-settings=imagesEnabled=false");
            options.BinaryLocation = "/opt/google/chrome/chrome";

            log.Infomation("** BeginCreate ChromeDriver");
            using (var service = ChromeDriverService.CreateDefaultService(driverPath, driverExecutableFileName))
            using (var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(60)))
            {
                log.Infomation("** Created ChromeDriver");
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
                driver.Manage().Window.Minimize();

                var url = @"https://vtuber-insight.com/index.html";

                log.Infomation($"** GoToUrl({url})");
                driver.Navigate().GoToUrl(url);
                log.Infomation($"** Navigated {url}");

                for (int i = 20; i > 0; i--)
                {
                    log.Infomation($"** Wait {i} seconds for web socket");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }

                log.Infomation("get drive.PageSource");

                return driver.PageSource;
            }
        }
    }
}
