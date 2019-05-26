using System;

namespace Microsoft.HpcAcm.Common.Utilities
{
    public class CloudOptions
    {
        #region Storage

        public StorageConfiguration Storage { get; set; } = new StorageConfiguration();
        public string ArmMsiUri { get; set; } = "http://169.254.169.254/metadata/identity/oauth2/token?api-version=2018-02-01&resource=https%3A%2F%2Fmanagement.azure.com%2F";
        public string ArmListStorageKeyUri { get; set; } = "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Storage/storageAccounts/{2}/listKeys?api-version=2016-12-01";
        public string ArmComputeMetadataUri { get; set; } = "http://169.254.169.254/metadata/instance/compute?api-version=2017-08-01";
        public string ArmResourceGroupUri { get; set; } = "https://management.azure.com/subscriptions/{0}/resourcegroups/{1}?api-version=2018-02-01";

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
        public string ScheduledEventsKey { get; internal set; } = "scheduled-events";
        public string MetadataKey { get; internal set; } = "metadata-instance";
        public string RegistrationPattern { get; set; } = "registration-{0}";
        public string HeartbeatPattern { get; internal set; } = "heartbeat-{0}";
        public string NodesPartitionKey { get; internal set; } = "nodes";
        public string GroupsPartitionKey { get; internal set; } = "groups";
        public string GroupPattern { get; set; } = "group-{0}";

        public int HeartbeatIntervalSeconds { get; set; } = 30;
        public int MaxMissedHeartbeats { get; set; } = 3;
        public int RegistrationIntervalSeconds { get; set; } = 300;
        public int RetryOnFailureSeconds { get; set; } = 5;

        #endregion

        #region Management Operation table

        public string ManagementOperataionTableName { get; set; } = "managementoperationtable";
        public string ManagementRequestRowKey { get; internal set; } = "request";
        public string ManagementResponseRowKey { get; internal set; } = "response";

        #endregion

        #region Jobs table

        public string DashboardTableName { get; set; } = "dashboardtable";
        public string JobsTableName { get; set; } = "jobstable";
        public string JobEntryKey { get; internal set; } = "jobentry";
        public string EventsKeyPattern { get; internal set; } = "events-{0}";
        public string JobAggregationResultPattern { get; internal set; } = "aggregationresult-{0}";
        public string NodeTaskResultPattern { get; internal set; } = "nodejobresult-{0}-{1}";
        public string JobReversePartitionPattern { get; internal set; } = "jobreverse-{0}-{1}";
        public string JobPartitionPattern { get; internal set; } = "job-{0}-{1}";
        public string DiagnosticCategoryPattern { get; internal set; } = "diag-{0}";
        public string DashboardRowKeyPattern { get; internal set; } = "range-{0}";
        public string DashboardPartitionPattern { get; internal set; } = "dashboard-{0}";
        public string DashboardEntryKey { get; internal set; } = "entry";

        #endregion

        #region Blobs

        public string JobResultContainerPattern { get; set; } = "jobresults-{0}";
        public string TaskChildrenContainerName { get; set; } = "taskchildren";

        #endregion

        #region Queue

        public string RunningJobQueue { get; set; } = "runningjobqueue";
        public string JobTaskCompletionQueuePattern { get; set; } = "taskcompletionqueue-{0}";
        public string TaskCompletionQueueName { get; set; } = "taskcompletionqueue";
        public string JobEventQueueName { get; set; } = "jobeventqueue";
        public string ScriptSyncQueueName { get; set; } = "scriptsyncqueue";
        public string NodeDispatchQueuePattern { get; set; } = "nodedispatchqueue-{0}";
        public string NodeCancelQueuePattern { get; set; } = "nodecancelqueue-{0}";
        public string ManagementRequestQueue { get; set; } = "management-request";
        public string ManagementResponseQueue { get; set; } = "management-response-{0}";


        #endregion
    }
}
