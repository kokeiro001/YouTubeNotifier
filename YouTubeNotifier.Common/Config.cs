using System;

namespace YouTubeNotifier.Common
{
    public class YouTubeNotifyServiceConfig
    {
        public DateTime FromDateTimeUtc { get; set; }

        public DateTime ToDateTimeUtc { get; set; }

        public bool UseCache { get; set; }

        public AzureTableStorageConfig AzureTableStorageConfig { get; set; }
    }

    public class AzureTableStorageConfig
    {
        public string ConnectionString { get; set; }
    }
}
