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
                    var entity = await idsTable.RetrieveAsJsonAsync(category, usage, token);
                    if (entity == null)
                    {
                        entity = new JsonTableEntity() { PartitionKey = category, RowKey = usage, };
                    }
                    else
                    {
                        currentId = entity.GetObject<int>() + 1;
                    }

                    entity.PutObject(currentId);

                    var updateResult = await idsTable.InsertOrReplaceAsync(entity, token);

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

        public static async T.Task UpdateTaskAsync(this CloudUtilities u, string jobPartitionKey, string taskKey, Action<Task> action, CancellationToken token)
        {
            var jobTable = u.GetJobsTable();

            var task = await jobTable.RetrieveAsync<Task>(jobPartitionKey, taskKey, token);
            if (task != null)
            {
                action(task);
                await jobTable.InsertOrReplaceAsJsonAsync(jobPartitionKey, taskKey, task, token);
            }
        }

        public static async T.Task<bool> UpdateJobAsync(this CloudUtilities u, JobType type, int jobId, Action<Job> action, CancellationToken token)
        {
            var pKey = u.GetJobPartitionKey(type, jobId, true);
            bool result1 = await u.UpdateJobAsync(pKey, action, token);

            pKey = u.GetJobPartitionKey(type, jobId, false);
            bool result2 = await u.UpdateJobAsync(pKey, action, token);

            return result1 && result2;
        }

        private static async T.Task<bool> UpdateJobAsync(this CloudUtilities u, string jobPartitionKey, Action<Job> action, CancellationToken token)
        {
            var jobRowKey = u.JobEntryKey;

            var jobTable = u.GetJobsTable();

            var job = await jobTable.RetrieveAsync<Job>(jobPartitionKey, jobRowKey, token);
            if (job != null)
            {
                action(job);
                await jobTable.InsertOrReplaceAsJsonAsync(jobPartitionKey, jobRowKey, job, token);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
