using System;

namespace Microsoft.HpcAcm.Common.Utilities
{
    public class CloudOption
    {
        public string StorageKeyOrSas { get; set; }
        public string AccountName { get; set; }
        public string ConnectionString { get; set; }

        public int QueueServerTimeoutSeconds { get; set; } = 30;
        public int TableServerTimeoutSeconds { get; set; } = 30;
        public int VisibleTimeoutSeconds { get; set; } = 60;

        public string NodesTableName { get; set; } = "nodestable";
        public string JobsTableName { get; set; } = "jobstable";

        public string JobDispatchQueueName { get; set; } = "jobdispatchqueue";
        public string NodeDispatchQueuePattern { get; set; } = "nodedispatchqueue-{0}";

        public string JobPartitionPattern { get; internal set; } = "job-{0}";
        public string JobEntryKey { get; internal set; } = "jobentry";
        public string JobResultPattern { get; internal set; } = "nodejobresult-{0}";
    }
}
