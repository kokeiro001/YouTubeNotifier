using CoreTweet;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YouTubeNotifier.VTuberRankingCrawler
{
    class TwitterService
    {
        private readonly Tokens tokens;

        public TwitterService(Settings settings)
        {
            tokens = Tokens.Create(
                settings.Twitter.ApiKey,
                settings.Twitter.ApiSecret,
                settings.Twitter.AccessToken,
                settings.Twitter.AccessTokenSecret
            );
        }

        public async Task TweetGeneratedPlaylist(string playlistId, string playlistTitle, int videoCount)
        {
            var playlistUrl = $"https://www.youtube.com/playlist?list={playlistId}";

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{playlistTitle} ({videoCount}件登録済み)");

            stringBuilder.AppendLine("");
            stringBuilder.Append(playlistUrl);

            await tokens.Statuses.UpdateAsync(new
            {
                status = stringBuilder.ToString(),
            });
        }
    }
}
