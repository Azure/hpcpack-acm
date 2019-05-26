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
    using System.Net;
    using System.Text;
    using System.Threading;
    using Serilog;
    using T = System.Threading.Tasks;

    public class CloudUtilities
    {
        private readonly ILogger logger;

        public CloudUtilities(IOptions<CloudOptions> cloudOption, ILogger logger)
        {
            this.Option = cloudOption.Value;
            ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 12;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            this.logger = logger;
            this.logger.Information("Constructed cloud utilities");
        }

        public CloudStorageAccount Account { get; private set; }

        public bool IsSharedKeyAccount { get => this.Account.Credentials.IsSharedKey; }

        public async T.Task SetupStorageAccountAsync(CancellationToken token)
        {
            while (true)
            {
                try
                {
                    if (!string.IsNullOrEmpty(this.Option.Storage?.ConnectionString))
                    {
                        this.logger.Information("Setup storage account, using connection string");
                        // Connection String
                        this.Account = CloudStorageAccount.Parse(this.Option.Storage.ConnectionString);
                    }
                    else if (!string.IsNullOrEmpty(this.Option.Storage?.SasToken) && !string.IsNullOrEmpty(this.Option.Storage?.AccountName))
                    {
                        // SAS Token
                        this.logger.Information("Setup storage account, using SasToken, {0}", this.Option.Storage.AccountName);
                        this.Account = new CloudStorageAccount(
                            new WindowsAzure.Storage.Auth.StorageCredentials(this.Option.Storage.SasToken),
                            this.Option.Storage.AccountName,
                            null,
                            true);
                    }
                    else if (!string.IsNullOrEmpty(this.Option.Storage?.KeyValue) && !string.IsNullOrEmpty(this.Option.Storage?.AccountName))
                    {
                        // account key
                        this.logger.Information("Setup storage account, using Storage key, {0}", this.Option.Storage.AccountName);
                        this.Account = new CloudStorageAccount(
                            new WindowsAzure.Storage.Auth.StorageCredentials(this.Option.Storage.AccountName, this.Option.Storage.KeyValue),
                            true);
                    }
                    else
                    {
                        // MSI
                        this.logger.Information("Setup storage account, using MSI");
                        var msiClient = new ManagedServiceIdentityClient(this.Option, this.logger);
                        var config = await msiClient.GetStorageConfigAsync(token);

                        if (config != null)
                        {
                            this.Account = new CloudStorageAccount(
                                new WindowsAzure.Storage.Auth.StorageCredentials(config.AccountName, config.KeyValue),
                                true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Getting error when creating the cloud storage account");
                }

                if (this.Account != null) break;
                await T.Task.Delay(5000, token);
            }

            this.blobClient = new CloudBlobClient(Account.BlobEndpoint, Account.Credentials);
            this.queueClient = new CloudQueueClient(Account.QueueEndpoint, Account.Credentials);
            this.tableClient = new CloudTableClient(Account.TableEndpoint, Account.Credentials);
            queueClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(this.Option.QueueServerTimeoutSeconds);
            tableClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(this.Option.TableServerTimeoutSeconds);
        }

        public async T.Task InitializeAsync(CancellationToken token)
        {
            await this.SetupStorageAccountAsync(token);
            await this.GetOrCreateJobEventQueueAsync(token);
            await this.GetOrCreateJobsTableAsync(token);
            await this.GetOrCreateNodesTableAsync(token);
            await this.GetOrCreateIdsTableAsync(token);
            await this.GetOrCreateMetricsTableAsync(token);
        }

        public CloudOptions Option { get; private set; }
        public readonly string MaxString = new string(Char.MaxValue, 1);
        public readonly string MinString = new string(Char.MinValue, 1);

        private CloudBlobClient blobClient;
        private CloudQueueClient queueClient;
        private CloudTableClient tableClient;

        public const string PartitionKeyName = "PartitionKey";
        public const string RowKeyName = "RowKey";

        public string GetRowKeyRangeString(string lowKey, string highKey, bool leftInclusive = false, bool rightInclusive = true) => GetKeyRangeString(lowKey, highKey, RowKeyName, leftInclusive, rightInclusive);
        public string GetPartitionKeyRangeString(string lowKey, string highKey, bool leftInclusive = false, bool rightInclusive = true) => GetKeyRangeString(lowKey, highKey, PartitionKeyName, leftInclusive, rightInclusive);

        public string GetKeyRangeString(string lowKey, string highKey, string keyName, bool leftInclusive, bool rightInclusive) => TableQuery.CombineFilters(
            TableQuery.GenerateFilterCondition(keyName, leftInclusive ? QueryComparisons.GreaterThanOrEqual : QueryComparisons.GreaterThan, lowKey),
            TableOperators.And,
            TableQuery.GenerateFilterCondition(keyName, rightInclusive ? QueryComparisons.LessThanOrEqual : QueryComparisons.LessThan, highKey));

        public string GetPartitionQueryString(string partitionKey) =>
            TableQuery.GenerateFilterCondition(PartitionKeyName, QueryComparisons.Equal, partitionKey);

        public string GetJobPartitionKey(JobType type, int jobId, bool reverse = false) => this.GetJobPartitionKey(type.ToString().ToLowerInvariant(), jobId, reverse);
        public string GetJobPartitionKey(string type, int jobId, bool reverse = false) =>
            reverse ? string.Format(this.Option.JobReversePartitionPattern, type, IntegerKey.ToStringKey(int.MaxValue - jobId)) : string.Format(this.Option.JobPartitionPattern, type, IntegerKey.ToStringKey(jobId));
        public string GetDashboardRowKey(string rowId) => string.Format(this.Option.DashboardRowKeyPattern, rowId);
        public string GetDashboardEntryKey() => this.Option.DashboardEntryKey;
        public string GetDashboardPartitionKey(string category) => string.Format(this.Option.DashboardPartitionPattern, category);
        public string GetDiagPartitionKey(string category) => string.Format(this.Option.DiagnosticCategoryPattern, category);
        public string GetDiagCategoryName(string partitionKey) => partitionKey.Substring(5);
        public string GetNodePartitionKey(string nodeName) => string.Format(this.Option.NodePartitionPattern, nodeName);
        public string GetMinuteHistoryKey(long minutes) => string.Format(this.Option.MinuteHistoryPattern, IntegerKey.ToStringKey(minutes));
        public string GetMinuteHistoryKey() => this.Option.MinuteHistoryKey;
        public string GetMetadataKey() => this.Option.MetadataKey;
        public string GetScheduledEventsKey() => this.Option.ScheduledEventsKey;
        public string GetRegistrationKey(string nodeName) => string.Format(this.Option.RegistrationPattern, nodeName);
        public string GetMaximumRegistrationKey() => string.Format(this.Option.RegistrationPattern, this.MaxString);
        public string GetHeartbeatKey(string nodeName) => string.Format(this.Option.HeartbeatPattern, nodeName);
        public string NodesPartitionKey { get => this.Option.NodesPartitionKey; }
        public string GroupsPartitionKey { get => this.Option.GroupsPartitionKey; }
        public string GetGroupKey(int groupId) => string.Format(this.Option.GroupPattern, IntegerKey.ToStringKey(groupId));

        public string JobEntryKey { get => this.Option.JobEntryKey; }
        public string GetEventsKey(long ticks) => string.Format(this.Option.EventsKeyPattern, IntegerKey.ToStringKey(ticks));
        public string MetricsValuesPartitionKey { get => this.Option.MetricsValuesPartitionKey; }
        public string MetricsCategoriesPartitionKey { get => this.Option.MetricsCategoriesPartitionKey; }

        public string GetNodeTaskResultKey(string nodeKey, int jobId, int requeueCount, int taskId) => string.Format(this.Option.NodeTaskResultPattern, nodeKey, this.GetRawTaskKey(jobId, taskId, requeueCount));
        public string GetMinimumNodeTaskResultKey() => this.GetNodeTaskResultKey(this.MinString, 0, 0, 0);
        public string GetMaximumNodeTaskResultKey() => this.GetNodeTaskResultKey(this.MaxString, int.MaxValue, int.MaxValue, int.MaxValue);
        public string GetTaskKey(int jobId, int taskId, int requeueCount) => $"task-{this.GetRawTaskKey(jobId, taskId, requeueCount)}";
        public string GetTaskInfoKey(int jobId, int taskId, int requeueCount) => $"taskinfo-{this.GetRawTaskKey(jobId, taskId, requeueCount)}";
        public string GetMinimumTaskKey(int jobId, int requeueCount) => this.GetTaskKey(jobId, 0, requeueCount);
        public string GetMaximumTaskKey(int jobId, int requeueCount) => this.GetTaskKey(jobId, int.MaxValue, requeueCount);
        public string GetRawTaskKey(int jobId, int taskId, int requeueCount) => $"{IntegerKey.ToStringKey(jobId)}-{IntegerKey.ToStringKey(requeueCount)}-{IntegerKey.ToStringKey(taskId)}";
        public string GetTaskResultKey(int jobId, int taskId, int requeueCount) => $"taskresult-{this.GetRawTaskKey(jobId, taskId, requeueCount)}";
        public string GetJobAggregationResultKey(int jobId) => string.Format(this.Option.JobAggregationResultPattern, jobId);

        public CloudQueue GetQueue(string queueName) => this.queueClient.GetQueueReference(queueName);

        public async T.Task<CloudQueue> GetOrCreateQueueAsync(string queueName, CancellationToken token)
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

        public async T.Task<CloudBlockBlob> UploadToBlockBlobAsync(string containerName, string blobName, string content, CancellationToken token)
        {
            var jobContainer = this.blobClient.GetContainerReference(containerName);
            await jobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null, token);
            var blob = jobContainer.GetBlockBlobReference(blobName);
            await blob.UploadTextAsync(content, Encoding.UTF8, null, null, null, token);
            return blob;
        }

        public async T.Task<CloudAppendBlob> GetOrCreateAppendBlobAsync(string containerName, string blobName, CancellationToken token)
        {
            var jobContainer = this.blobClient.GetContainerReference(containerName);
            await jobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, token);
            var blob = jobContainer.GetAppendBlobReference(blobName);
            await blob.CreateOrReplaceAsync(null, null, null, token);
            return blob;
        }

        public CloudTable GetTable(string tableName) => this.tableClient.GetTableReference(tableName);

        public async T.Task<CloudTable> GetOrCreateTableAsync(string tableName, CancellationToken token)
        {
            var t = this.GetTable(tableName);
            await t.CreateIfNotExistsAsync(null, null, token);
            return t;
        }
    }
}
