using System;

namespace YouTubeNotifier.Common
{
    public enum StorageType
    {
        LocalStorage,
        AzureTableStorage,
    }

    public class YouTubeNotifyServiceConfig
    {
        public DateTime FromDateTimeUtc { get; set; }

        public DateTime ToDateTimeUtc { get; set; }

        public bool UseCache { get; set; }

        public StorageType StorageType { get; set; }

        public LocalStorageConfig LocalStorageConfig { get; set; }

        public AzureTableStorageConfig AzureTableStorageConfig { get; set; }
    }

    public class LocalStorageConfig
    {
        public string ClientSecretFilePath { get; set; }

        public string CredentialsDirectory { get; set; }
    }

    public class AzureTableStorageConfig
    {
        public string ConnectionString { get; set; }
    }
}
