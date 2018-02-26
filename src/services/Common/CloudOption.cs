namespace Microsoft.HpcAcm.Services.Common
{
    public class CloudOption
    {
        public string StorageKeyOrSas { get; set; }
        public int QueueServerTimeoutSeconds { get; set; }
        public int TableServerTimeoutSeconds { get; set; }
        public int QueueCount { get; set; }
        public string TaskCompletionQueueName { get; set; }
        public string RegisterTableName { get; set; }
        public string HeartbeatTableName { get; set; }
    }
}
