﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
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
            
            var fromUtc = DateTime.UtcNow.AddDays(-1);

            var movieIds = new List<string>();
            foreach (var channelInfo in targetYouTubeChannelIds)
            {
                var channelMovieIds = await GetUploadedMovies(channelInfo.Id, fromUtc);
                movieIds.AddRange(channelMovieIds);
            }

            // create today playlist
            var japanNow = DateTime.UtcNow.AddHours(9);
            var insertPlaylistRequest = youTubeService.Playlists.Insert(new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = japanNow.ToString("yyyy年M月dd日 H時m分s秒"),
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "private",
                },
            },"snippet,status");

            var insertPlaylistResponse = await insertPlaylistRequest.ExecuteAsync();

            // insert movies
            foreach (var movieId in movieIds)
            {
                var insertPlaylistItemRequest = youTubeService.PlaylistItems.Insert(new PlaylistItem
                {
                    Snippet = new PlaylistItemSnippet
                    {
                        PlaylistId = insertPlaylistResponse.Id,
                        ResourceId = new ResourceId
                        {
                            Kind = "youtube#video",
                            VideoId = movieId,
                        }
                    },
                }, "snippet");
                await insertPlaylistItemRequest.ExecuteAsync();
            }
        }

        private async Task<List<ChannelInfo>> GetSubscriptionYouTubeChannels()
        {
            // TODO: DBとかにキャッシュしておけば平和になる

            var list = new List<ChannelInfo>();
            var pageToken = default(string);

            do
            {
                var subscriptionsListRequest = youTubeService.Subscriptions.List("snippet");
                subscriptionsListRequest.Fields = "nextPageToken,items/snippet/title,items/snippet/resourceId/channelId";
                subscriptionsListRequest.Mine = true;
                subscriptionsListRequest.MaxResults = 5;
                subscriptionsListRequest.PageToken = pageToken;

                var subscriptionList = await subscriptionsListRequest.ExecuteAsync();

                foreach (var subscription in subscriptionList.Items)
                {
                    Console.WriteLine(subscription.Snippet.Title);
                    list.Add(new ChannelInfo
                    {
                        Id = subscription.Snippet.ResourceId.ChannelId,
                        Title = subscription.Snippet.Title,
                    });
                }

                pageToken = subscriptionList.NextPageToken;
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
            searchRequest.MaxResults = 5;
            var data = await searchRequest.ExecuteAsync();

            // TODO: loop if all movie added.
            foreach (var item in data.Items)
            {
                if (item.Id.Kind != "youtube#video")
                {
                    continue;
                }

                var publishedAt = DateTime.Parse(item.Snippet.PublishedAtRaw, null, DateTimeStyles.RoundtripKind);
                
                if (publishedAt >= from)
                {
                    list.Add(item.Id.VideoId);
                }
            }

            return list;
        }

        private class ChannelInfo
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }
    }
}
