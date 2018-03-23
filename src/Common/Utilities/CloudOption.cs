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

        #region Metrics table

        public string MetricsTableName { get; set; } = "metricstable";

        // TODO: scale out by metric category
        public string MetricsValuesPartitionKey { get; set; } = "metricsvalues";
        public string MetricsCategoriesPartitionKey { get; set; } = "metricscategories";

        #endregion

        #region Ids table

        public string IdsTableName { get; set; } = "idstable";

        #endregion

        #region Nodes table

        public string NodesTableName { get; set; } = "nodestable";
        public string NodePartitionPattern { get; internal set; } = "node-{0}";
        public string MinuteHistoryPattern { get; internal set; } = "history-minute-{0}";
        public string MinuteHistoryKey { get; internal set; } = "history-minute";
        public string RegistrationPattern { get; set; } = "registration-{0}";
        public string HeartbeatPattern { get; internal set; } = "heartbeat-{0}";
        public string NodesPartitionKey { get; internal set; } = "nodes";

        public int HeartbeatIntervalSeconds { get; set; } = 30;
        public int MaxMissedHeartbeats { get; set; } = 3;
        public int RegistrationIntervalSeconds { get; set; } = 300;
        public int RetryOnFailureSeconds { get; set; } = 5;

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
