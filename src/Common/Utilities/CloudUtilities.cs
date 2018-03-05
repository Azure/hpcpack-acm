namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage;
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
            var account = new CloudStorageAccount(new WindowsAzure.Storage.Auth.StorageCredentials(cloudOption.StorageKeyOrSas), "evanc", "", true);
       
            //var account = CloudStorageAccount.Parse(cloudOption.StorageKeyOrSas);
            this.queueClient = new CloudQueueClient(account.QueueEndpoint, account.Credentials);
            this.tableClient = new CloudTableClient(account.TableEndpoint, account.Credentials);
            queueClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(cloudOption.QueueServerTimeoutSeconds);
            tableClient.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(cloudOption.TableServerTimeoutSeconds);
        }

        public CloudOption Option { get; private set; }

        private CloudQueueClient queueClient;
        private CloudTableClient tableClient;

        public string GetJobPartitionName(int jobId) => string.Format(this.Option.JobPartitionPattern, jobId);
        public string JobEntryKey { get => this.Option.JobEntryKey; }

        public string GetJobResultKey(string nodeKey) => string.Format(this.Option.JobResultPattern, nodeKey);

        public async Task<CloudQueue> GetOrCreateJobDispatchQueueAsync(CancellationToken token)
        {
            return await this.GetOrCreateQueueAsync(this.Option.JobDispatchQueueName, token);
        }

        public async Task<CloudQueue> GetOrCreateNodeDispatchQueueAsync(string nodeName, CancellationToken token)
        {
            return await this.GetOrCreateQueueAsync(string.Format(this.Option.NodeDispatchQueuePattern, nodeName), token);
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
