using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
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

            // TODO: read youtube channel uploaded movies


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
                subscriptionsListRequest.MaxResults = 50;
                subscriptionsListRequest.PageToken = pageToken;

                var subscriptionList = await subscriptionsListRequest.ExecuteAsync();

                foreach (var subscription in subscriptionList.Items)
                {
                    Console.WriteLine(subscription.Snippet.Title);
                    list.Add((subscription.Id, subscription.Snippet.Title));
                }

                pageToken = subscriptionList.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return list;
        }
    }
}
