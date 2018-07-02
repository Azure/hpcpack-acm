namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public static class CloudUtilitiesExtensions
    {
        public static CloudTable GetIdsTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.IdsTableName);
        }
        public static async T.Task<CloudTable> GetOrCreateIdsTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.IdsTableName, token);
        }
        public static async T.Task<int> GetNextId(this CloudUtilities u, string category, string usage, CancellationToken token)
        {
            var idsTable = u.GetIdsTable();

            while (true)
            {
                try
                {
                    int currentId = 1;
                    var entity = await idsTable.RetrieveJsonTableEntityAsync(category, usage, token);
                    TableResult result;
                    if (entity == null)
                    {
                        entity = new JsonTableEntity() { PartitionKey = category, RowKey = usage, };
                        entity.PutObject(currentId);
                        result = await idsTable.InsertAsync(entity, token);
                    }
                    else
                    {
                        currentId = entity.GetObject<int>() + 1;
                        entity.PutObject(currentId);
                        result = await idsTable.ReplaceAsync(entity, token);
                    }

                    // concurrency failure or conflict
                    if (result.IsConflict()) continue;

                    return currentId;
                }
                catch (StorageException ex)
                {
                    // concurrency failure or conflict
                    if (ex.RequestInformation.IsConflict()) continue;
                    throw;
                }
            }
        }

        public static CloudTable GetJobsTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.JobsTableName);
        }

        public static async T.Task<CloudTable> GetOrCreateJobsTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.JobsTableName, token);
        }

        public static async T.Task<CloudAppendBlob> CreateOrReplaceJobOutputBlobAsync(this CloudUtilities u, JobType jobType, string key, CancellationToken token)
        {
            return await u.GetOrCreateAppendBlobAsync(
                string.Format(u.Option.JobResultContainerPattern, jobType.ToString().ToLowerInvariant()),
                key,
                token);
        }

        public static CloudAppendBlob GetJobOutputBlob(this CloudUtilities u, JobType jobType, string key) => u.GetAppendBlob(
            string.Format(u.Option.JobResultContainerPattern, jobType.ToString().ToLowerInvariant()),
            key);

        public static CloudQueue GetJobEventQueue(this CloudUtilities u) => u.GetQueue(u.Option.JobEventQueueName);

        public static async T.Task<CloudQueue> GetOrCreateTaskCompletionQueueAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(u.Option.TaskCompletionQueueName, token);
        }

        public static async T.Task<CloudQueue> GetOrCreateJobEventQueueAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(u.Option.JobEventQueueName, token);
        }

        public static async T.Task<CloudQueue> GetOrCreateNodeDispatchQueueAsync(this CloudUtilities u, string nodeName, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(string.Format(u.Option.NodeDispatchQueuePattern, nodeName), token);
        }

        public static async T.Task<CloudQueue> GetOrCreateNodeCancelQueueAsync(this CloudUtilities u, string nodeName, CancellationToken token)
        {
            return await u.GetOrCreateQueueAsync(string.Format(u.Option.NodeCancelQueuePattern, nodeName), token);
        }

        public static CloudTable GetMetricsTable(this CloudUtilities u) => u.GetTable(u.Option.MetricsTableName);

        public static async T.Task<CloudTable> GetOrCreateMetricsTableAsync(this CloudUtilities u, CancellationToken token) => await u.GetOrCreateTableAsync(u.Option.MetricsTableName, token);

        public static CloudTable GetNodesTable(this CloudUtilities u)
        {
            return u.GetTable(u.Option.NodesTableName);
        }

        public static async T.Task<CloudTable> GetOrCreateNodesTableAsync(this CloudUtilities u, CancellationToken token)
        {
            return await u.GetOrCreateTableAsync(u.Option.NodesTableName, token);
        }

        public static T.Task<bool> UpdateTaskAsync(this CloudUtilities u, string jobPartitionKey, string taskKey, Action<Task> action, CancellationToken token) =>
            u.UpdateObjectAsync(u.GetJobsTable(), jobPartitionKey, taskKey, action, token);

        public static async T.Task<bool> UpdateJobAsync(this CloudUtilities u, JobType type, int jobId, Action<Job> action, CancellationToken token)
        {
            var pKey = u.GetJobPartitionKey(type, jobId, true);
            bool result1 = await u.UpdateJobAsync(pKey, action, token);

            pKey = u.GetJobPartitionKey(type, jobId, false);
            bool result2 = await u.UpdateJobAsync(pKey, action, token);

            return result1 && result2;
        }

        private static T.Task<bool> UpdateJobAsync(this CloudUtilities u, string jobPartitionKey, Action<Job> action, CancellationToken token) =>
            u.UpdateObjectAsync(u.GetJobsTable(), jobPartitionKey, u.JobEntryKey, action, token);

        public static async T.Task<bool> UpdateObjectAsync<TObject>(this CloudUtilities u, CloudTable table, string partitionKey, string rowKey, Action<TObject> action, CancellationToken token)
        {
            while (true)
            {
                var entity = await table.RetrieveJsonTableEntityAsync(partitionKey, rowKey, token);
                var obj = entity.GetObject<TObject>();

                if (obj != null)
                {
                    action(obj);
                    entity.PutObject(obj);
                    var result = await table.ReplaceAsync(entity, token);
                    if (result.IsConflict()) continue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
