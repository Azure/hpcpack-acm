namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public static class CloudUtilitiesExtensions
    {
        public static CloudTable GetIdsTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.IdsTableName);
        }
        public static async Task<CloudTable> GetOrCreateIdsTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.IdsTableName, token);
        }
        public static async Task<int> GetNextId(this CloudUtilities u, string category, string usage, CancellationToken token)
        {
            var idsTable = u.GetIdsTable();

            while (true)
            {
                try
                {
                    var result = await idsTable.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(category, usage), null, null, token);

                    int currentId = 1;
                    if (result.Result is JsonTableEntity id)
                    {
                        currentId = id.GetObject<int>() + 1;
                    }
                    else
                    {
                        id = new JsonTableEntity() { PartitionKey = category, RowKey = usage, };
                    }

                    id.JsonContent = JsonConvert.SerializeObject(currentId);

                    var updateResult = await idsTable.ExecuteAsync(TableOperation.InsertOrReplace(id), null, null, token);

                    // concurrency failure or conflict
                    if (updateResult.HttpStatusCode == 412 || updateResult.HttpStatusCode == 409)
                    {
                        continue;
                    }

                    return currentId;
                }
                catch (StorageException ex)
                {
                    // concurrency failure or conflict
                    if (ex.RequestInformation.HttpStatusCode != 412 && ex.RequestInformation.HttpStatusCode != 409)
                    {
                        throw;
                    }
                }
            }
        }

        public static CloudTable GetJobsTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.JobsTableName);
        }

        public static async Task<CloudTable> GetOrCreateJobsTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.JobsTableName, token);
        }

        public static async Task<CloudAppendBlob> CreateOrReplaceTaskOutputBlobAsync(this CloudUtilities u, int jobId, string key, CancellationToken token)
        {
            return await u.GetOrCreateAppendBlobAsync(
                string.Format(u.Option.JobResultContainerPattern, IntegerKey.ToStringKey(jobId)),
                key,
                token);
        }

        public static CloudAppendBlob GetTaskOutputBlob(this CloudUtilities u, int jobId, string key) => u.GetAppendBlob(
            string.Format(u.Option.JobResultContainerPattern, IntegerKey.ToStringKey(jobId)),
            key);

        public static CloudQueue GetJobDispatchQueue(this CloudUtilities u) => u.GetQueue(u.Option.JobDispatchQueueName);

        public static async Task<CloudQueue> GetOrCreateJobDispatchQueueAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(u.Option.JobDispatchQueueName, token);
        }

        public static async Task<CloudQueue> GetOrCreateNodeDispatchQueueAsync(this CloudUtilities u, string nodeName, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(string.Format(u.Option.NodeDispatchQueuePattern, nodeName), token);
        }

        public static CloudTable GetMetricsTable(this CloudUtilities u) => u.GetTable(u.Option.MetricsTableName);

        public static async Task<CloudTable> GetOrCreateMetricsTableAsync(this CloudUtilities u, CancellationToken token) => await u.GetOrCreateTableAsync(u.Option.NodesTableName, token);

        public static CloudTable GetNodesTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.NodesTableName);
        }

        public static async Task<CloudTable> GetOrCreateNodesTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.NodesTableName, token);
        }
    }
}
