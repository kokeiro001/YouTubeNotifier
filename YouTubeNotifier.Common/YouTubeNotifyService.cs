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
using YouTubeNotifier.Common.Repository;

namespace YouTubeNotifier.Common
{
    // なんかいいnuget探してきたほうがいい
    public interface IMyLogger
    {
        void Infomation(string message);

        void Warning(string message);

        void Error(string message);
    }

    public class YouTubeNotifyService
    {
        private static readonly string[] Scopes = { YouTubeService.Scope.Youtube };
        private static readonly string ApplicationName = "YouTubeNotifier";

        private readonly YouTubeNotifyServiceConfig config;
        private readonly IMyLogger log;
        private YouTubeService youTubeService;

        public YouTubeNotifyService(YouTubeNotifyServiceConfig config, IMyLogger log)
        {
            this.config = config;
            this.log = log;
        }

        private async Task CreateYoutubeService()
        {
            if (youTubeService != null)
            {
                throw new Exception($"already created {nameof(youTubeService)}");
            }

            UserCredential credential;

            if (config.StorageType == StorageType.LocalStorage)
            {
                credential = await GetCredentialFromLocalFile();
            }
            else if(config.StorageType == StorageType.AzureTableStorage)
            {
                credential = await GetCredentialFromTableStoraeg();
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
            log.Infomation("CreateYoutubeService");
            await CreateYoutubeService();

            var fromDateTimeJst = config.FromDateTimeUtc.AddHours(9);
            log.Infomation($"fromDateTimeJst={fromDateTimeJst}");

            log.Infomation($"GetOrInsertPlaylist({fromDateTimeJst})");
            var insertPlaylistResponse = await GetOrInsertPlaylist(fromDateTimeJst);

            if (config.UseCache)
            {
                throw new NotSupportedException(nameof(config.UseCache));
            }
            log.Infomation($"GetSubscriptionYouTubeChannels");
            var targetYouTubeChannelIds = await GetSubscriptionYouTubeChannels(false);
            log.Infomation($"targetYouTubeChannelIds.Count={targetYouTubeChannelIds.Count}");

            var fromUtc = config.FromDateTimeUtc;
            var toUtc = config.ToDateTimeUtc;

            var movieIds = new List<string>();
            foreach (var channelInfo in targetYouTubeChannelIds)
            {
                log.Infomation($"GetUploadedMovies({channelInfo.Id}, {fromUtc}, {toUtc})");
                var channelMovieIds = await GetUploadedMovies(channelInfo.Id, fromUtc, toUtc);

                log.Infomation($"channelMovieIds.Count={channelMovieIds.Count}");
                movieIds.AddRange(channelMovieIds);
            }
            log.Infomation($"movieIds.Count={movieIds.Count}");


            log.Infomation($"Insert Movies");
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
                log.Infomation($"insertPlaylistItemRequest VideoId={movieId}");

                await insertPlaylistItemRequest.ExecuteAsync();
            }
        }

        private async Task<Playlist> GetOrInsertPlaylist(DateTime fromDateTimeJst)
        {
            var pageToken = default(string);

            var playlistTitle = fromDateTimeJst.ToString("yyyy年M月dd日");

            var page = 0;
            var maxPage = 1;

            do
            {
                var listPlaylistRequest = youTubeService.Playlists.List("id,snippet");
                listPlaylistRequest.Mine = true;
                listPlaylistRequest.MaxResults = 5;
                listPlaylistRequest.PageToken = pageToken;
                listPlaylistRequest.Fields = "nextPageToken,items/id,items/snippet/title";

                log.Infomation("listPlaylistRequest.ExecuteAsync");
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

                log.Infomation($"listPlaylistResponse.NextPageToken={pageToken}, page={page}");
                log.Infomation($")!string.IsNullOrEmpty({pageToken}) && {page} <= {maxPage}) == {!string.IsNullOrEmpty(pageToken) && page <= maxPage}");
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

            log.Infomation("insertPlaylistRequest.ExecuteAsync");
            var insertPlaylistResponse = await insertPlaylistRequest.ExecuteAsync();
            return insertPlaylistResponse;
        }

        private async Task<List<Subscription>> GetSubscriptionYouTubeChannels(bool getTitle)
        {
            var list = new List<Subscription>();
            var pageToken = default(string);

            do
            {
                var subscriptionsListRequest = youTubeService.Subscriptions.List("snippet");
                subscriptionsListRequest.Fields = "nextPageToken,items/snippet/resourceId/channelId";
                subscriptionsListRequest.Mine = true;
                subscriptionsListRequest.MaxResults = 50;
                subscriptionsListRequest.PageToken = pageToken;

                if (getTitle)
                {
                    subscriptionsListRequest.Fields += ",items/snippet/title";
                }

                log.Infomation("subscriptionsListRequest.ExecuteAsync");
                var subscriptionList = await subscriptionsListRequest.ExecuteAsync();

                foreach (var subscription in subscriptionList.Items)
                {
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

                log.Infomation("searchRequest.ExecuteAsync");
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

        private Task<UserCredential> GetCredentialFromLocalFile()
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

        private async Task<UserCredential> GetCredentialFromTableStoraeg()
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

        public async Task UpdateSubscriptionChannelList(string categoryName)
        {
            var repository = new SubscriptionChannelRepository(config.AzureTableStorageConfig.ConnectionString);

            await CreateYoutubeService();

            var channelList = await GetSubscriptionYouTubeChannels(true);

            foreach (var channel in channelList)
            {
                await repository.AddOrInsert(categoryName, channel.Snippet.ChannelId, channel.Snippet.Title);
            }
        }
    }
}
