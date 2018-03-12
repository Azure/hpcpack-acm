using System;

namespace Microsoft.HpcAcm.Common.Utilities
{
    public class CloudOption
    {
        #region Credential

        public string StorageKeyOrSas { get; set; }
        public string AccountName { get; set; }
        public string ConnectionString { get; set; }

        #endregion

        #region Server operations

        public int QueueServerTimeoutSeconds { get; set; } = 30;
        public int TableServerTimeoutSeconds { get; set; } = 30;
        public int VisibleTimeoutSeconds { get; set; } = 60;

        #endregion

        #region Nodes table

        public string NodesTableName { get; set; } = "nodestable";
        public string NodePartitionPattern { get; internal set; } = "node-{0}";
        public string HeartbeatPattern { get; internal set; } = "heartbeat-{0}";
        public string NodesPartitionKey { get; internal set; } = "nodes";

        #endregion

        #region Jobs table

        public string JobsTableName { get; set; } = "jobstable";
        public string JobEntryKey { get; internal set; } = "jobentry";
        public string JobResultPattern { get; internal set; } = "nodejobresult-{0}-{1}";
        public string JobPartitionPattern { get; internal set; } = "job-{0}-{1}";

        #endregion

        #region Blobs

        public string JobResultContainerPattern { get; set; } = "jobresults-{0}";

        #endregion

        #region Queue

        public string JobDispatchQueueName { get; set; } = "jobdispatchqueue";
        public string NodeDispatchQueuePattern { get; set; } = "nodedispatchqueue-{0}";

        #endregion
    }
}
