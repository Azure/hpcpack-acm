namespace Microsoft.HpcAcm.Common.Utilities
{
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
        public CloudUtilities(CloudOption cloudOption)
        {
            this.Option = cloudOption;

            account = string.IsNullOrEmpty(this.Option.ConnectionString) ?
                new CloudStorageAccount(
                    new WindowsAzure.Storage.Auth.StorageCredentials(cloudOption.StorageKeyOrSas),
                    this.Option.AccountName,
                    null,
                    true)
                : CloudStorageAccount.Parse(this.Option.ConnectionString);

            this.blobClient = new CloudBlobClient(account.BlobEndpoint, account.Credentials);
            this.queueClient = new CloudQueueClient(account.QueueEndpoint, account.Credentials);
            this.tableClient = new CloudTableClient(account.TableEndpoint, account.Credentials);
            queueClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(cloudOption.QueueServerTimeoutSeconds);
            tableClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(cloudOption.TableServerTimeoutSeconds);
        }

        private readonly CloudStorageAccount account;

        public bool IsSharedKeyAccount { get => this.account.Credentials.IsSharedKey; }

        public async Task InitializeAsync(CancellationToken token)
        {
            await this.GetOrCreateJobDispatchQueueAsync(token);
            await this.GetOrCreateJobsTableAsync(token);
            await this.GetOrCreateNodesTableAsync(token);
            await this.GetOrCreateIdsTableAsync(token);
            await this.GetOrCreateMetricsTableAsync(token);
        }

        public CloudOption Option { get; private set; }
        public readonly string MaxString = new string(Char.MaxValue, 1);
        public readonly string MinString = new string(Char.MinValue, 1);

        private readonly CloudBlobClient blobClient;
        private readonly CloudQueueClient queueClient;
        private readonly CloudTableClient tableClient;

        public const string PartitionKeyName = "PartitionKey";
        public const string RowKeyName = "RowKey";

        public string GetRowKeyRangeString(string lowRowKey, string highRowKey) => TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition(RowKeyName, QueryComparisons.GreaterThan, lowRowKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition(RowKeyName, QueryComparisons.LessThan, highRowKey));

        public string GetPartitionQueryString(string partitionKey) =>
            TableQuery.GenerateFilterCondition(PartitionKeyName, QueryComparisons.Equal, partitionKey);

        public string GetJobPartitionKey(string type, int jobId) => string.Format(this.Option.JobPartitionPattern, type, IntegerKey.ToStringKey(jobId));
        public string GetNodePartitionKey(string nodeName) => string.Format(this.Option.NodePartitionPattern, nodeName);
        public string GetRegistrationKey(string nodeName) => string.Format(this.Option.RegistrationPattern, nodeName);
        public string GetMaximumRegistrationKey() => string.Format(this.Option.RegistrationPattern, this.MaxString);
        public string GetHeartbeatKey(string nodeName) => string.Format(this.Option.HeartbeatPattern, nodeName);
        public string NodesPartitionKey { get => this.Option.NodesPartitionKey; }
        public string JobEntryKey { get => this.Option.JobEntryKey; }
        public string MetricsValuesPartitionKey { get => this.Option.MetricsValuesPartitionKey; }
        public string MetricsCategoriesPartitionKey { get => this.Option.MetricsCategoriesPartitionKey; }

        public string GetJobResultKey(string nodeKey, string taskKey) => string.Format(this.Option.JobResultPattern, nodeKey, taskKey);
        public string GetMinimumJobResultKey() => string.Format(this.Option.JobResultPattern, this.MinString, null);
        public string GetMaximumJobResultKey() => string.Format(this.Option.JobResultPattern, this.MaxString, null);
        public string GetTaskKey(int jobId, int taskId, int requeueCount) => $"{IntegerKey.ToStringKey(jobId)}-{IntegerKey.ToStringKey(taskId)}-{IntegerKey.ToStringKey(requeueCount)}";

        public CloudQueue GetQueue(string queueName) => this.queueClient.GetQueueReference(queueName);

        public async Task<CloudQueue> GetOrCreateQueueAsync(string queueName, CancellationToken token)
        {
            var q = this.GetQueue(queueName);
            await q.CreateIfNotExistsAsync(null, null, token);
            return q;
        }
        public CloudBlobContainer GetContainer(string containerName) => this.blobClient.GetContainerReference(containerName);

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
