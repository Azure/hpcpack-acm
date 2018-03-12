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

            var account = string.IsNullOrEmpty(this.Option.ConnectionString) ?
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

        public CloudOption Option { get; private set; }

        private readonly CloudBlobClient blobClient;
        private readonly CloudQueueClient queueClient;
        private readonly CloudTableClient tableClient;

        public string GetJobPartitionKey(int jobId, string type) => string.Format(this.Option.JobPartitionPattern, type, jobId);
        public string GetNodePartitionKey(string nodeName) => string.Format(this.Option.NodePartitionPattern, nodeName);
        public string GetHeartbeatKey(string nodeName) => string.Format(this.Option.HeartbeatPattern, nodeName);
        public string NodesPartitionKey { get => this.Option.NodesPartitionKey; }
        public string JobEntryKey { get => this.Option.JobEntryKey; }

        public string GetJobResultKey(string nodeKey, string taskKey) => string.Format(this.Option.JobResultPattern, nodeKey, taskKey);
        public string GetTaskKey(int jobId, int taskId, int requeueCount) => $"{jobId}:{taskId}:{requeueCount}";

        public CloudQueue GetQueue(string queueName) => this.queueClient.GetQueueReference(queueName);

        public async Task<CloudQueue> GetOrCreateQueueAsync(string queueName, CancellationToken token)
        {
            var q = this.GetQueue(queueName);
            await q.CreateIfNotExistsAsync(null, null, token);
            return q;
        }
        public CloudBlobContainer GetContainer(string containerName) => this.blobClient.GetContainerReference(containerName);

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
