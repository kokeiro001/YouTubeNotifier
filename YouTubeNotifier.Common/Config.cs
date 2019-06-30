using System;

namespace YouTubeNotifier.Common
{
    public class YouTubeNotifyServiceConfig
    {
        public DateTime FromDateTimeUtc { get; set; }

        public DateTime ToDateTimeUtc { get; set; }

        public bool UseCache { get; set; }

        public string AzureTableStorageConnectionString { get; set; }
    }
}
