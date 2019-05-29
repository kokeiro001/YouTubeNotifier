using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

    class YouTubeNotifyService
    {
        private static readonly string[] Scopes = { YouTubeService.Scope.Youtube };
        private static readonly string ApplicationName = "YouTubeNotifier";
        private static readonly string ClientSecretFilePath = @"youtubenotifier_client_id.json";
        private static readonly CultureInfo EnUsInfo = new System.Globalization.CultureInfo("en-US");

        private YouTubeService youTubeService;

        private void CreateYoutubeService()
        {
            if (youTubeService != null)
            {
                return;
            }

            var credential = default(UserCredential);

            using (var stream = new FileStream(ClientSecretFilePath, FileMode.Open, FileAccess.Read))
            {
                var credPath = "Credentials";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public async Task Run()
        {
            CreateYoutubeService();

            var targetYouTubeChannelIds = await GetSubscriptionYouTubeChannels();

            var fromUtc = DateTime.UtcNow.AddDays(-7);

            var movieIds = new List<string>();
            foreach (var (youtubeChannelId, title) in targetYouTubeChannelIds)
            {
                var channelMovieIds = await GetUploadedMovies(youtubeChannelId, fromUtc);
                channelMovieIds.AddRange(channelMovieIds);
                break;
            }

            // TODO: create today playlist

            // TODO: add new movies to playlist
        }

        private async Task<List<(string id, string channelTitle)>> GetSubscriptionYouTubeChannels()
        {
            var list = new List<(string id, string channelTitle)>();
            var pageToken = default(string);
            do
            {
                var subscriptionsListRequest = youTubeService.Subscriptions.List("snippet");
                subscriptionsListRequest.Mine = true;
                subscriptionsListRequest.MaxResults = 5;
                subscriptionsListRequest.PageToken = pageToken;

                var subscriptionList = await subscriptionsListRequest.ExecuteAsync();

                foreach (var subscription in subscriptionList.Items)
                {
                    Console.WriteLine(subscription.Snippet.Title);
                    list.Add((subscription.Snippet.ResourceId.ChannelId, subscription.Snippet.Title));
                }

                pageToken = subscriptionList.NextPageToken;
                break;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return list;
        }

        private async Task<List<string>> GetUploadedMovies(string channelId, DateTime from)
        {
            var list = new List<string>();

            var searchRequest = youTubeService.Search.List("snippet");
            searchRequest.ChannelId = channelId;
            searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
            var data = await searchRequest.ExecuteAsync();

            foreach (var item in data.Items)
            {
                var publishedAt = DateTime.Parse(item.Snippet.PublishedAtRaw, null, System.Globalization.DateTimeStyles.RoundtripKind);
                if (publishedAt >= from)
                {
                    list.Add(item.Id.VideoId);
                }
            }

            return list;
        }

    }
}
