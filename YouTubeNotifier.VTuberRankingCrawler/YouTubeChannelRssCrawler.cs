using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class YouTubeChannelRssCrawler
    {
        private readonly Log4NetLogger log;

        public YouTubeChannelRssCrawler(Log4NetLogger log)
        {
            this.log = log;
        }

        public async Task<YouTubeRssItem[]> GetUploadedMovies(string[] youtubeChannelIds, DateTime fromUtc, DateTime toUtc)
        {
            var list = new List<YouTubeRssItem>();

            foreach (var youtubeChannelId in youtubeChannelIds.Take(300))
            {
                log.Infomation($"GetRssItems({youtubeChannelId})");

                var tmp = GetRssItems(youtubeChannelId);

                var targetMovieIds = tmp
                    .Where(x => x.PublishDateUtc >= fromUtc)
                    .Where(x => x.PublishDateUtc <= toUtc);

                list.AddRange(targetMovieIds);

                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            return list.ToArray();
        }

        private List<YouTubeRssItem> GetRssItems(string channelId)
        {
            var url = $"https://www.youtube.com/feeds/videos.xml?channel_id={channelId}";

            var list = new List<YouTubeRssItem>();

            using (var xmlReader = XmlReader.Create(url))
            {
                var feed = SyndicationFeed.Load(xmlReader);
                foreach (var item in feed.Items)
                {
                    var movieUri = item.Links.First().Uri;
                    var movieId = movieUri.Query.Substring(3);

                    var youtubeRssItem = new YouTubeRssItem
                    {
                        Url = movieUri.ToString(),
                        MovieId = movieId,
                        Title = item.Title.Text,
                        PublishDateUtc = item.PublishDate.DateTime,
                    };

                    list.Add(youtubeRssItem);
                }
            }

            return list;
        }
    }


    class YouTubeRssItem
    {
        public string Url { get; set; }

        public string MovieId { get; set; }

        public string Title { get; set; }

        public DateTime PublishDateUtc { get; set; }
    }
}
