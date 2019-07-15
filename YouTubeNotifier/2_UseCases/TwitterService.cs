using CoreTweet;
using System.Text;
using System.Threading.Tasks;

namespace YouTubeNotifier.UseCases
{
    class TwitterService
    {
        private readonly Tokens tokens;

        public TwitterService(Settings.TwitterSettings settings)
        {
            tokens = Tokens.Create(
                settings.ApiKey,
                settings.ApiSecret,
                settings.AccessToken,
                settings.AccessTokenSecret
            );
        }

        public async Task TweetGeneratedPlaylist(string playlistId, string playlistTitle, int videoCount)
        {
            var playlistUrl = $"https://www.youtube.com/playlist?list={playlistId}";

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"再生リスト「{playlistTitle}」を作成しました。({videoCount}件登録済み)");

            stringBuilder.AppendLine("");
            stringBuilder.Append(playlistUrl);

            await tokens.Statuses.UpdateAsync(new
            {
                status = stringBuilder.ToString(),
            });
        }
    }
}
