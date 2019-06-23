using AngleSharp.Html.Parser;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                var html = await GetPageSource();

                List<Item> items = Parse(html);

                foreach (var item in items)
                {
                    Console.WriteLine($"{item.Rank:D3} {item.ChannelName}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} {e.StackTrace}");
            }
        }

        private static List<Item> Parse(string html)
        {
            var htmlDocument = new HtmlParser().ParseDocument(html);

            var workspace = htmlDocument.QuerySelectorAll("div")
                .Where(x => x.Id == "workspace")
                .First();

            var table = workspace.QuerySelectorAll("table")
                .Where(x => x.GetAttribute("border") == "1")
                .First();

            var items = new List<Item>();

            foreach (var tr in table.QuerySelectorAll("tr").Skip(2))
            {
                var tds = tr.QuerySelectorAll("td").ToArray();

                items.Add(new Item
                {
                    Rank = int.Parse(tds[1].TextContent),
                    ChannelName = tds[3].TextContent,
                });
            }

            return items;
        }

        private static async Task<string> GetPageSource()
        {
            // setup
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

                // test method
                var url = @"https://vtuber-insight.com/index.html";

                driver.Navigate().GoToUrl(url);

                await Task.Delay(TimeSpan.FromSeconds(30));

                return driver.PageSource;
            }
        }
    }

    class Item
    {
        public int Rank { get; set; }
        public string ChannelName { get; set; }
    }
}
