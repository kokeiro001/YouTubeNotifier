namespace YouTubeNotifier.Common
{
    // なんかいいnuget探してきたほうがいい
    public interface IMyLogger
    {
        void Infomation(string message);

        void Warning(string message);

        void Error(string message);
    }
}
