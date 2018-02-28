using System;

namespace Microsoft.HpcAcm.Common.Utilities
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

        public string JobDispatchQueueName { get; set; }
        public string NodeDispatchQueuePattern { get; set; }
        public string DiagnosticsJobTableName { get; set; }
        public int VisibleTimeoutSeconds { get; set; }
        public string JobPartitionPattern { get; internal set; } = "Job-{0}";
        public string JobEntryKey { get; internal set; } = "JobEntry";
        public string JobResultPattern { get; internal set; } = "Node-{0}";
    }
}
