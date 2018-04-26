namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class CloudUtilities
    {
        public CloudUtilities(IOptions<CloudOptions> cloudOption)
        {
            this.Option = cloudOption.Value;

            account = string.IsNullOrEmpty(this.Option.ConnectionString) ?
                new CloudStorageAccount(
                    new WindowsAzure.Storage.Auth.StorageCredentials(this.Option.StorageKeyOrSas),
                    this.Option.AccountName,
                    null,
                    true)
                : CloudStorageAccount.Parse(this.Option.ConnectionString);

            this.blobClient = new CloudBlobClient(account.BlobEndpoint, account.Credentials);
            this.queueClient = new CloudQueueClient(account.QueueEndpoint, account.Credentials);
            this.tableClient = new CloudTableClient(account.TableEndpoint, account.Credentials);
            queueClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(this.Option.QueueServerTimeoutSeconds);
            tableClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(this.Option.TableServerTimeoutSeconds);
        }

        private readonly CloudStorageAccount account;

        public bool IsSharedKeyAccount { get => this.account.Credentials.IsSharedKey; }

        public async Task InitializeAsync(CancellationToken token)
        {
            await this.GetOrCreateJobEventQueueAsync(token);
            await this.GetOrCreateJobsTableAsync(token);
            await this.GetOrCreateNodesTableAsync(token);
            await this.GetOrCreateIdsTableAsync(token);
            await this.GetOrCreateMetricsTableAsync(token);
        }

        public CloudOptions Option { get; private set; }
        public readonly string MaxString = new string(Char.MaxValue, 1);
        public readonly string MinString = new string(Char.MinValue, 1);

        private readonly CloudBlobClient blobClient;
        private readonly CloudQueueClient queueClient;
        private readonly CloudTableClient tableClient;

        public const string PartitionKeyName = "PartitionKey";
        public const string RowKeyName = "RowKey";

        public string GetRowKeyRangeString(string lowKey, string highKey) => GetKeyRangeString(lowKey, highKey, RowKeyName);
        public string GetPartitionKeyRangeString(string lowKey, string highKey) => GetKeyRangeString(lowKey, highKey, PartitionKeyName);

        public string GetKeyRangeString(string lowKey, string highKey, string keyName) => TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition(keyName, QueryComparisons.GreaterThan, lowKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition(keyName, QueryComparisons.LessThan, highKey));

        public string GetPartitionQueryString(string partitionKey) =>
            TableQuery.GenerateFilterCondition(PartitionKeyName, QueryComparisons.Equal, partitionKey);

        public string GetJobPartitionKey(JobType type, int jobId, bool reverse = false) => this.GetJobPartitionKey(type.ToString().ToLowerInvariant(), jobId, reverse);
        public string GetJobPartitionKey(string type, int jobId, bool reverse = false) =>
            reverse ? string.Format(this.Option.JobReversePartitionPattern, type, IntegerKey.ToStringKey(int.MaxValue - jobId)) : string.Format(this.Option.JobPartitionPattern, type, IntegerKey.ToStringKey(jobId));
        public string GetDiagPartitionKey(string category) => string.Format(this.Option.DiagnosticCategoryPattern, category);
        public string GetDiagCategoryName(string partitionKey) => partitionKey.Substring(5);
        public string GetNodePartitionKey(string nodeName) => string.Format(this.Option.NodePartitionPattern, nodeName);
        public string GetMinuteHistoryKey(long minutes) => string.Format(this.Option.MinuteHistoryPattern, IntegerKey.ToStringKey(minutes));
        public string GetMinuteHistoryKey() => string.Format(this.Option.MinuteHistoryKey);
        public string GetRegistrationKey(string nodeName) => string.Format(this.Option.RegistrationPattern, nodeName);
        public string GetMaximumRegistrationKey() => string.Format(this.Option.RegistrationPattern, this.MaxString);
        public string GetHeartbeatKey(string nodeName) => string.Format(this.Option.HeartbeatPattern, nodeName);
        public string NodesPartitionKey { get => this.Option.NodesPartitionKey; }
        public string JobEntryKey { get => this.Option.JobEntryKey; }
        public string MetricsValuesPartitionKey { get => this.Option.MetricsValuesPartitionKey; }
        public string MetricsCategoriesPartitionKey { get => this.Option.MetricsCategoriesPartitionKey; }

        public string GetNodeTaskResultKey(string nodeKey, int jobId, int requeueCount, int taskId) => string.Format(this.Option.NodeTaskResultPattern, nodeKey, this.GetRawTaskKey(jobId, taskId, requeueCount));
        public string GetMinimumNodeTaskResultKey() => this.GetNodeTaskResultKey(this.MinString, 0, 0, 0);
        public string GetMaximumNodeTaskResultKey() => this.GetNodeTaskResultKey(this.MaxString, int.MaxValue, int.MaxValue, int.MaxValue);
        public string GetTaskKey(int jobId, int taskId, int requeueCount) => $"task-{this.GetRawTaskKey(jobId, taskId, requeueCount)}";
        public string GetMinimumTaskKey(int jobId, int requeueCount) => this.GetTaskKey(jobId, 0, requeueCount);
        public string GetMaximumTaskKey(int jobId, int requeueCount) => this.GetTaskKey(jobId, int.MaxValue, requeueCount);
        public string GetRawTaskKey(int jobId, int taskId, int requeueCount) => $"{IntegerKey.ToStringKey(jobId)}-{IntegerKey.ToStringKey(requeueCount)}-{IntegerKey.ToStringKey(taskId)}";
        public string GetTaskResultKey(int jobId, int taskId, int requeueCount) => $"taskresult-{this.GetRawTaskKey(jobId, taskId, requeueCount)}";

        public CloudQueue GetQueue(string queueName) => this.queueClient.GetQueueReference(queueName);

        public async Task<CloudQueue> GetOrCreateQueueAsync(string queueName, CancellationToken token)
        {
            var q = this.GetQueue(queueName);
            await q.CreateIfNotExistsAsync(null, null, token);
            return q;
        }
        public CloudBlobContainer GetContainer(string containerName) => this.blobClient.GetContainerReference(containerName);

        public CloudBlob GetBlob(string containerName, string blobName)
        {
            var container = this.GetContainer(containerName);
            return container.GetBlobReference(blobName);
        }

        public CloudAppendBlob GetAppendBlob(string containerName, string blobName)
        {
            var jobContainer = this.blobClient.GetContainerReference(containerName);
            return jobContainer.GetAppendBlobReference(blobName);
        }

        public async Task<CloudAppendBlob> GetOrCreateAppendBlobAsync(string containerName, string blobName, CancellationToken token)
        {
            var jobContainer = this.blobClient.GetContainerReference(containerName);
            await jobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, token);
            var blob = jobContainer.GetAppendBlobReference(blobName);
            await blob.CreateOrReplaceAsync(null, null, null, token);
            return blob;
        }

        public CloudTable GetTable(string tableName) => this.tableClient.GetTableReference(tableName);

        public async Task<CloudTable> GetOrCreateTableAsync(string tableName, CancellationToken token)
        {
            var t = this.GetTable(tableName);
            await t.CreateIfNotExistsAsync(null, null, token);
            return t;
        }
    }
}
