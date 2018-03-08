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

        public string GetJobPartitionName(int jobId, string type) => string.Format(this.Option.JobPartitionPattern, type, jobId);
        public string GetNodePartitionName(string nodeName) => string.Format(this.Option.NodePartitionPattern, nodeName);
        public string JobEntryKey { get => this.Option.JobEntryKey; }

        public string GetJobResultKey(string nodeKey, string taskKey) => string.Format(this.Option.JobResultPattern, nodeKey, taskKey);
        public string GetTaskKey(int jobId, int taskId, int requeueCount) => $"{jobId}:{taskId}:{requeueCount}";

        public async Task<CloudAppendBlob> CreateOrReplaceTaskOutputBlobAsync(int jobId, string key, CancellationToken token)
        {
            var jobContainer = this.blobClient.GetContainerReference(string.Format(this.Option.JobResultContainerPattern, jobId));
            await jobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, token);
            var blob = jobContainer.GetAppendBlobReference(key);
            await blob.CreateOrReplaceAsync(null, null, null, token);
            return blob;
        }


        public async Task<CloudQueue> GetOrCreateJobDispatchQueueAsync(CancellationToken token)
        {
            return await this.GetOrCreateQueueAsync(this.Option.JobDispatchQueueName, token);
        }

        public async Task<CloudQueue> GetOrCreateNodeDispatchQueueAsync(string nodeName, CancellationToken token)
        {
            return await this.GetOrCreateQueueAsync(string.Format(this.Option.NodeDispatchQueuePattern, nodeName), token);
        }

        public async Task<CloudTable> GetOrCreateNodesTableAsync(CancellationToken token)
        {
            return await this.GetOrCreateTableAsync(this.Option.NodesTableName, token);
        }


        public async Task<CloudTable> GetOrCreateJobsTableAsync(CancellationToken token)
        {
            return await this.GetOrCreateTableAsync(this.Option.JobsTableName, token);
        }

        private async Task<CloudQueue> GetOrCreateQueueAsync(string queueName, CancellationToken token)
        {
            var q = this.queueClient.GetQueueReference(queueName);
            await q.CreateIfNotExistsAsync(null, null, token);
            return q;
        }

        private async Task<CloudTable> GetOrCreateTableAsync(string tableName, CancellationToken token)
        {
            var t = this.tableClient.GetTableReference(tableName);
            await t.CreateIfNotExistsAsync(null, null, token);
            return t;
        }
    }
}
