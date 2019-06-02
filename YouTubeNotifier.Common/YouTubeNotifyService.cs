using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YouTubeNotifier.Common
{
    public class YouTubeNotifyService
    {
        private static readonly string[] Scopes = { YouTubeService.Scope.Youtube };
        private static readonly string ApplicationName = "YouTubeNotifier";

        private readonly YouTubeNotifyServiceConfig config;

        private YouTubeService youTubeService;

        public YouTubeNotifyService(YouTubeNotifyServiceConfig config)
        {
            this.config = config;
        }

        private async Task CreateYoutubeService()
        {
            if (youTubeService != null)
            {
                throw new Exception($"already created {nameof(youTubeService)}");
            }


            var credential = default(UserCredential);

            if (config.StorageType == StorageType.LocalStorage)
            {
                credential = await GetCredentialByLocalFile();
            }
            else if(config.StorageType == StorageType.AzureTableStorage)
            {
                credential = await GetCredentialByTableStoraeg();
            }
            else
            {
                throw new Exception($"unexpedted type config.StorageType={config.StorageType}");
            }

            youTubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });
        }

        public async Task Run()
        {
            await CreateYoutubeService();

            var japanNow = DateTime.UtcNow.AddHours(9);

            var targetYouTubeChannelIds = default(List<ChannelInfo>);

            if (config.UseCache)
            {
                targetYouTubeChannelIds = await Util.Cache(async () =>
                {
                    return await GetSubscriptionYouTubeChannels();
                },
                japanNow.ToString("yyyyMMdd"));
            }
            else
            {
                targetYouTubeChannelIds = await GetSubscriptionYouTubeChannels();
            }

            var fromUtc = DateTime.UtcNow.AddDays(-1);

            var movieIds = new List<string>();
            foreach (var channelInfo in targetYouTubeChannelIds)
            {
                var channelMovieIds = await GetUploadedMovies(channelInfo.Id, fromUtc);
                movieIds.AddRange(channelMovieIds);
            }

            // TODO: get playlist, if exists, use it .

            // create today playlist
            var insertPlaylistRequest = youTubeService.Playlists.Insert(new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    //Title = japanNow.ToString("yyyy年M月dd日 H時m分s秒"),
                    Title = japanNow.ToString("yyyy年M月dd日"),
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "private",
                },
            }, "snippet,status");

            insertPlaylistRequest.Fields = "id";

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

                insertPlaylistItemRequest.Fields = "";

                await insertPlaylistItemRequest.ExecuteAsync();
            }
        }

        private async Task<List<ChannelInfo>> GetSubscriptionYouTubeChannels()
        {
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

            var pageToken = default(string);

            do
            {
                var searchRequest = youTubeService.Search.List("id");
                searchRequest.ChannelId = channelId;
                searchRequest.Order = SearchResource.ListRequest.OrderEnum.Date;
                searchRequest.MaxResults = 5;
                searchRequest.Fields = "nextPageToken,items/id/kind,items/id/videoId";
                searchRequest.PublishedAfter = from;
                searchRequest.Type = "video";
                searchRequest.PageToken = pageToken;

                var searchResponse = await searchRequest.ExecuteAsync();

                foreach (var item in searchResponse.Items)
                {
                    if (item.Id.Kind != "youtube#video")
                    {
                        continue;
                    }

                    list.Add(item.Id.VideoId);
                }

                pageToken = searchResponse.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return list;
        }

        private Task<UserCredential> GetCredentialByLocalFile()
        {
            var clientSecretFilePath = config.LocalStorageConfig.ClientSecretFilePath;

            using (var stream = new FileStream(clientSecretFilePath, FileMode.Open, FileAccess.Read))
            {
                var dataStore = new FileDataStore(config.LocalStorageConfig.CredentialsDirectory, true);

                return GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    dataStore
                );
            }
        }

        private async Task<UserCredential> GetCredentialByTableStoraeg()
        {
            var cloudStorageAccountConnectionString = config.AzureTableStorageConfig.ConnectionString;

            var secretStore = new TableStorageSecretStore(cloudStorageAccountConnectionString);
            var secretText = await secretStore.GetSecret();

            using (var stream = secretText.ToMemoryStream(Encoding.UTF8))
            {
                var dataStore = new TableStorageDataStore(cloudStorageAccountConnectionString);

                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    dataStore
                );
            }
        }

        public class ChannelInfo
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }
    }
}
