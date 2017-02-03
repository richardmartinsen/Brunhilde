using Microsoft.Azure;

namespace StatefulSemanticReddit
{
    public static class Config
    {
        public static string OcpApimSubscriptionKey => CloudConfigurationManager.GetSetting("OcpApimSubscriptionKey");
        public static string EhConnectionString => CloudConfigurationManager.GetSetting(nameof(EhConnectionString));
        public static string StorageContainerName => CloudConfigurationManager.GetSetting(nameof(StorageContainerName));
        public static string StorageAccountName => CloudConfigurationManager.GetSetting(nameof(StorageAccountName));
        public static string StorageAccountKey => CloudConfigurationManager.GetSetting(nameof(StorageAccountKey));
        public static string OffsetFileName => CloudConfigurationManager.GetSetting(nameof(OffsetFileName));
        public static string StorageConnectionString => $"DefaultEndpointsProtocol=https;AccountName={StorageAccountName};AccountKey={StorageAccountKey}";
        public static string EhAnalyticPath => "analytics";
        public static string EhCommentPath => "reddit-comments";
    }
}
