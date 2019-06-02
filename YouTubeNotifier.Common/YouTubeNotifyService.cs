using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


            var fromDateTimeJst = config.FromDateTimeUtc.AddHours(9);

            // create today playlist
            var insertPlaylistResponse = await GetOrInsertPlaylist(fromDateTimeJst);

            var targetYouTubeChannelIds = default(List<Subscription>);

            if (config.UseCache)
            {
                targetYouTubeChannelIds = await Util.Cache(async () =>
                {
                    return await GetSubscriptionYouTubeChannels();
                },
                fromDateTimeJst.ToString("yyyyMMdd"));
            }
            else
            {
                targetYouTubeChannelIds = await GetSubscriptionYouTubeChannels();
            }

            var fromUtc = config.FromDateTimeUtc;
            var toUtc = config.ToDateTimeUtc;

            var movieIds = new List<string>();
            foreach (var channelInfo in targetYouTubeChannelIds)
            {
                var channelMovieIds = await GetUploadedMovies(channelInfo.Id, fromUtc, toUtc);
                movieIds.AddRange(channelMovieIds);
            }

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

        private async Task<Playlist> GetOrInsertPlaylist(DateTime fromDateTimeJst)
        {
            var pageToken = default(string);

            var playlistTitle = fromDateTimeJst.ToString("yyyy年M月dd日");

            var page = 0;
            var maxPage = 3;

            do
            {
                var listPlaylistRequest = youTubeService.Playlists.List("id,snippet");
                listPlaylistRequest.Mine = true;
                listPlaylistRequest.MaxResults = 5;
                listPlaylistRequest.PageToken = pageToken;
                listPlaylistRequest.Fields = "nextPageToken,items/id,items/snippet/title";

                var listPlaylistResponse = await listPlaylistRequest.ExecuteAsync();

                var playlist = listPlaylistResponse.Items
                    .Where(x => x.Snippet.Title == playlistTitle)
                    .FirstOrDefault();

                if (playlist != null)
                {
                    return playlist;
                }

                pageToken = listPlaylistResponse.NextPageToken;
                page++;

            } while (!string.IsNullOrEmpty(pageToken) && page <= maxPage);

            // not found playlist. insert playlist.

            var insertPlaylistRequest = youTubeService.Playlists.Insert(new Playlist
            {
                Snippet = new PlaylistSnippet
                {
                    Title = playlistTitle,
                },
                Status = new PlaylistStatus
                {
                    PrivacyStatus = "private",
                },
            }, "snippet,status");

            insertPlaylistRequest.Fields = "id";

            var insertPlaylistResponse = await insertPlaylistRequest.ExecuteAsync();
            return insertPlaylistResponse;
        }

        private async Task<List<Subscription>> GetSubscriptionYouTubeChannels()
        {
            var list = new List<Subscription>();
            var pageToken = default(string);

            do
            {
                var showTitle = false;

                var subscriptionsListRequest = youTubeService.Subscriptions.List("snippet");
                subscriptionsListRequest.Fields = "nextPageToken,items/snippet/resourceId/channelId";
                subscriptionsListRequest.Mine = true;
                subscriptionsListRequest.MaxResults = 5;
                subscriptionsListRequest.PageToken = pageToken;

                if (showTitle)
                {
                    subscriptionsListRequest.Fields += ",items/snippet/title";
                }

                var subscriptionList = await subscriptionsListRequest.ExecuteAsync();

                foreach (var subscription in subscriptionList.Items)
                {
                    if (showTitle)
                    {
                        Console.WriteLine(subscription.Snippet.Title);
                    }
                    list.Add(subscription);
                }

                pageToken = subscriptionList.NextPageToken;
            }
            while (!string.IsNullOrEmpty(pageToken));

            return list;
        }

        private async Task<List<string>> GetUploadedMovies(string channelId, DateTime from, DateTime to)
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
                searchRequest.PublishedBefore = to;
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
    }
}
