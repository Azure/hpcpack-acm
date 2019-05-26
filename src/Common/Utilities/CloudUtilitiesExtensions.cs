namespace Microsoft.HpcAcm.Common.Utilities
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using Newtonsoft.Json;
    using Serilog;
    using Serilog.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public static class CloudUtilitiesExtensions
    {
        public static CloudTable GetIdsTable(this CloudUtilities u) => u.GetTable(u.Option.IdsTableName);
        public static T.Task<CloudTable> GetOrCreateIdsTableAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateTableAsync(u.Option.IdsTableName, token);

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
                    if (ex.IsConflict()) continue;
                    throw;
                }
            }
        }

        public static CloudTable GetJobsTable(this CloudUtilities u) => u.GetTable(u.Option.JobsTableName);

        public static T.Task<CloudTable> GetOrCreateDashboardTableAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateTableAsync(u.Option.DashboardTableName, token);

        public static T.Task<CloudTable> GetOrCreateJobsTableAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateTableAsync(u.Option.JobsTableName, token);

        public static T.Task<CloudAppendBlob> CreateOrReplaceJobOutputBlobAsync(this CloudUtilities u, JobType jobType, string key, CancellationToken token) =>
            u.GetOrCreateAppendBlobAsync(
                string.Format(u.Option.JobResultContainerPattern, jobType.ToString().ToLowerInvariant()),
                key,
                token);

        public static T.Task<CloudAppendBlob> CreateOrReplaceTaskChildrenBlobAsync(this CloudUtilities u, string key, CancellationToken token) =>
            u.GetOrCreateAppendBlobAsync(u.Option.TaskChildrenContainerName, key, token);

        public static CloudAppendBlob GetJobOutputBlob(this CloudUtilities u, JobType jobType, string key) => u.GetAppendBlob(
            string.Format(u.Option.JobResultContainerPattern, jobType.ToString().ToLowerInvariant()),
            key);

        public static CloudQueue GetJobEventQueue(this CloudUtilities u) => u.GetQueue(u.Option.JobEventQueueName);
        public static CloudQueue GetScriptSyncQueue(this CloudUtilities u) => u.GetQueue(u.Option.ScriptSyncQueueName);

        public static CloudQueue GetRunningJobQueue(this CloudUtilities u) => u.GetQueue(u.Option.RunningJobQueue);

        public static CloudQueue GetTaskCompletionQueue(this CloudUtilities u) => u.GetQueue(u.Option.TaskCompletionQueueName);
        public static CloudQueue GetJobTaskCompletionQueue(this CloudUtilities u, int jobId) => u.GetQueue(string.Format(u.Option.JobTaskCompletionQueuePattern, jobId));

        public static T.Task<CloudQueue> GetOrCreateScriptSyncQueueAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateQueueAsync(u.Option.ScriptSyncQueueName, token);
        public static T.Task<CloudQueue> GetOrCreateRunningJobQueueAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateQueueAsync(u.Option.RunningJobQueue, token);

        public static T.Task<CloudQueue> GetOrCreateTaskCompletionQueueAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateQueueAsync(u.Option.TaskCompletionQueueName, token);

        public static T.Task<CloudQueue> GetOrCreateJobTaskCompletionQueueAsync(this CloudUtilities u, int jobId, CancellationToken token) => u.GetOrCreateQueueAsync(string.Format(u.Option.JobTaskCompletionQueuePattern, jobId), token);

        public static T.Task<CloudQueue> GetOrCreateJobEventQueueAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateQueueAsync(u.Option.JobEventQueueName, token);

        public static CloudQueue GetNodeDispatchQueue(this CloudUtilities u, string nodeName) => u.GetQueue(string.Format(u.Option.NodeDispatchQueuePattern, nodeName));

        public static T.Task<CloudQueue> GetOrCreateNodeDispatchQueueAsync(this CloudUtilities u, string nodeName, CancellationToken token) => u.GetOrCreateQueueAsync(string.Format(u.Option.NodeDispatchQueuePattern, nodeName), token);

        public static CloudQueue GetNodeCancelQueue(this CloudUtilities u, string nodeName) => u.GetQueue(string.Format(u.Option.NodeCancelQueuePattern, nodeName));

        public static T.Task<CloudQueue> GetOrCreateNodeCancelQueueAsync(this CloudUtilities u, string nodeName, CancellationToken token) => u.GetOrCreateQueueAsync(string.Format(u.Option.NodeCancelQueuePattern, nodeName), token);

        public static T.Task<CloudQueue> GetOrCreateManagementRequestQueueAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateQueueAsync(u.Option.ManagementRequestQueue, token);

        public static T.Task<CloudQueue> GetOrCreateManagementResponseQueueAsync(this CloudUtilities u, string Id, CancellationToken token) => u.GetOrCreateQueueAsync(string.Format(u.Option.ManagementResponseQueue, Id), token);

        public static CloudTable GetMetricsTable(this CloudUtilities u) => u.GetTable(u.Option.MetricsTableName);

        public static T.Task<CloudTable> GetOrCreateMetricsTableAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateTableAsync(u.Option.MetricsTableName, token);

        public static CloudTable GetNodesTable(this CloudUtilities u) => u.GetTable(u.Option.NodesTableName);

        public static T.Task<CloudTable> GetOrCreateNodesTableAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateTableAsync(u.Option.NodesTableName, token);

        public static T.Task<CloudTable> GetOrCreateManagementOperationTableAsync(this CloudUtilities u, CancellationToken token) => u.GetOrCreateTableAsync(u.Option.ManagementOperataionTableName, token);

        public static T.Task<bool> UpdateTaskAsync(this CloudUtilities u, string jobPartitionKey, string taskKey, Action<Task> action, CancellationToken token, ILogger logger = null) =>
            u.UpdateObjectAsync(u.GetJobsTable(), jobPartitionKey, taskKey, action, token, logger);

        public static T.Task FailJobWithEventAsync(this CloudUtilities u, Job job, string message, CancellationToken token, ILogger logger = null)
        {
            return u.FailJobWithEventAsync(job.Type, job.Id, message, token, logger);
        }

        public static async T.Task FailJobWithEventAsync(this CloudUtilities u, JobType jobType, int jobId, string message, CancellationToken token, ILogger logger = null)
        {
            await T.Task.WhenAll(
                u.AddJobsEventAsync(jobType, jobId, message, EventType.Alert, token, logger),
                u.UpdateJobAsync(jobType, jobId, j => j.State = JobState.Failed, token, logger));
        }

        public static T.Task AddJobsEventAsync(this CloudUtilities u, Job job, string message, EventType type = EventType.Information, CancellationToken token = default(CancellationToken), ILogger logger = null) =>
            u.AddJobsEventAsync(job.Type, job.Id, message, type, token, logger);

        public static T.Task AddJobsEventAsync(this CloudUtilities u, JobType jobType, int jobId, string message, EventType type = EventType.Information, CancellationToken token = default(CancellationToken), ILogger logger = null) =>
            u.AddJobsEventAsync(jobType, jobId, new Event() { Content = message, Source = EventSource.Job, Type = type }, token, logger);

        public static T.Task AddJobsEventAsync(this CloudUtilities u, JobType jobType, int jobId, Event e, CancellationToken token, ILogger logger = null) =>
            u.GetJobsTable().InsertOrReplaceAsync(u.GetJobPartitionKey(jobType, jobId), u.GetEventsKey(e.Id), e, token);

        public static async T.Task<bool> UpdateJobAsync(this CloudUtilities u, JobType type, int jobId, Action<Job> action, CancellationToken token, ILogger logger = null)
        {
            var pKey = u.GetJobPartitionKey(type, jobId, true);
            bool result1 = await u.UpdateJobAsync(pKey, action, token, logger);

            pKey = u.GetJobPartitionKey(type, jobId, false);
            bool result2 = await u.UpdateJobAsync(pKey, action, token, logger);

            return result1 && result2;
        }

        private static T.Task<bool> UpdateJobAsync(this CloudUtilities u, string jobPartitionKey, Action<Job> action, CancellationToken token, ILogger logger = null) =>
            u.UpdateObjectAsync(u.GetJobsTable(), jobPartitionKey, u.JobEntryKey, action, token, logger);

        public static async T.Task<bool> UpdateObjectAsync<TObject>(
            this CloudUtilities u,
            CloudTable table,
            string partitionKey,
            string rowKey,
            Action<TObject> action,
            CancellationToken token,
            ILogger logger = null)
        {
            while (true)
            {
                var entity = await table.RetrieveJsonTableEntityAsync(partitionKey, rowKey, token);
                var obj = entity.GetObject<TObject>();

                if (obj != null)
                {
                    action(obj);
                    entity.PutObject(obj);
                    try
                    {
                        var result = await table.ReplaceAsync(entity, token);
                        if (result.IsConflict())
                        {
                            logger.Warning("----> Conflict replace {0} {1}", entity.PartitionKey, entity.RowKey);
                            await T.Task.Delay(new Random().Next(3000), token);
                            continue;
                        }
                    }
                    catch (StorageException ex) when (ex.IsCancellation())
                    {
                        throw ex.InnerException;
                    }
                    catch (StorageException ex) when (ex.IsConflict())
                    {
                        await T.Task.Delay(new Random().Next(3000), token);
                        continue;
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static async T.Task<IEnumerable<Node>> GetNodesAsync(this CloudUtilities u, string lowerNodeName, string higherNodeName, int? count, CancellationToken token)
        {
            var nodesTable = u.GetNodesTable();
            var partitionQuery = u.GetPartitionQueryString(u.NodesPartitionKey);

            var lastRegistrationKey = u.GetRegistrationKey(lowerNodeName);
            var registrationEnd = u.GetRegistrationKey(higherNodeName);
            var registrationRangeQuery = u.GetRowKeyRangeString(lastRegistrationKey, registrationEnd);

            var q = TableQuery.CombineFilters(
                partitionQuery,
                TableOperators.And,
                registrationRangeQuery);

            var registrations = (await nodesTable.QueryAsync<ComputeClusterRegistrationInformation>(q, count, token)).Select(r => r.Item3).ToList();

            if (!registrations.Any())
            {
                return new Node[0];
            }

            var firstHeartbeat = u.GetHeartbeatKey(registrations[0].NodeName.ToLowerInvariant());
            var lastHeartbeat = u.GetHeartbeatKey(registrations[registrations.Count - 1].NodeName.ToLowerInvariant());
            var heartbeatRangeQuery = u.GetRowKeyRangeString(firstHeartbeat, lastHeartbeat, true);

            q = TableQuery.CombineFilters(
                partitionQuery,
                TableOperators.And,
                heartbeatRangeQuery);

            var heartbeats = (await nodesTable.QueryAsync<ComputeClusterNodeInformation>(q, null, token)).ToDictionary(h => h.Item3.Name.ToLowerInvariant(), h => (h.Item3, h.Item4));

            return registrations.Select(r =>
            {
                var nodeName = r.NodeName.ToLowerInvariant();
                var node = new Node() { NodeRegistrationInfo = r, Name = nodeName, };

                if (heartbeats.TryGetValue(nodeName, out (ComputeClusterNodeInformation, DateTimeOffset) n))
                {
                    node.LastHeartbeatTime = n.Item2;
                    node.RunningJobCount = n.Item1.Jobs.Count;

                    if (n.Item2.AddSeconds(u.Option.MaxMissedHeartbeats * u.Option.HeartbeatIntervalSeconds) > DateTimeOffset.UtcNow)
                    {
                        node.Health = NodeHealth.OK;
                        node.State = NodeState.Online;
                        // TODO: adding events
                    }
                    else
                    {
                        node.Health = NodeHealth.Error;
                    }
                }

                return node;
            });
        }

        public static async T.Task<IEnumerable<Event>> GetEventsAsync(
            this CloudUtilities u,
            CloudTable table,
            string partitionKey,
            string lowRowKey,
            string highRowKey,
            int count = 100,
            bool reverse = false,
            CancellationToken token = default(CancellationToken))
        {
            var q = TableQuery.CombineFilters(
                u.GetPartitionQueryString(partitionKey),
                TableOperators.And,
                u.GetRowKeyRangeString(lowRowKey, highRowKey));

            var results = await table.QueryAsync<Event>(q, count, token);
            return results.Select(r => r.Item3);
        }


        public static async T.Task<IEnumerable<Job>> GetJobsAsync(
            this CloudUtilities u,
            string lowPartitionKey,
            string highPartitionKey,
            int count = 100,
            JobType type = JobType.ClusRun,
            bool reverse = false,
            CancellationToken token = default(CancellationToken))
        {
            var jobTable = u.GetJobsTable();

            var partitionRange = u.GetPartitionKeyRangeString(lowPartitionKey, highPartitionKey);
            var rowKey = u.JobEntryKey;

            var q = TableQuery.CombineFilters(
                partitionRange,
                TableOperators.And,
                TableQuery.GenerateFilterCondition(CloudUtilities.RowKeyName, QueryComparisons.Equal, rowKey));

            var results = await jobTable.QueryAsync<Job>(q, count, token);
            return results.Select(r => { r.Item3.UpdatedAt = r.Item4; return r.Item3; });
        }

        public static T.Task<IEnumerable<Job>> GetJobsAsync(
            this CloudUtilities u,
            int lastId,
            int higherId = int.MaxValue,
            int count = 100,
            JobType type = JobType.ClusRun,
            bool reverse = false,
            CancellationToken token = default(CancellationToken))
        {
            lastId = reverse && lastId == 0 ? int.MaxValue : lastId;
            higherId = reverse && higherId == int.MaxValue ? 0 : higherId;

            var lowJobPartitionKey = u.GetJobPartitionKey(type, lastId, reverse);
            var highJobPartitionKey = u.GetJobPartitionKey(type, higherId, reverse);

            return u.GetJobsAsync(lowJobPartitionKey, highJobPartitionKey, count, type, reverse, token);
        }

        public static async T.Task<List<int>> LoadTaskChildIdsAsync(
            this CloudUtilities u,
            int taskId,
            int jobId,
            int jobRequeueCount,
            CancellationToken token)
        {
            var taskKey = u.GetTaskKey(jobId, taskId, jobRequeueCount);
            var childIdBlob = u.GetAppendBlob(u.Option.TaskChildrenContainerName, taskKey);
            var content = await childIdBlob.DownloadTextAsync(Encoding.UTF8, null, null, null, token);
            return JsonConvert.DeserializeObject<List<int>>(content);
        }

        public static async T.Task<IEnumerable<GroupWithNodes>> GetNodeGroupsAsync(this CloudUtilities utilities, CancellationToken token)
        {
            var table = await utilities.GetOrCreateNodesTableAsync(token);
            var query = TableQuery.GenerateFilterCondition(CloudUtilities.PartitionKeyName, QueryComparisons.Equal, utilities.GroupsPartitionKey);
            var result = await table.QueryAsync<GroupWithNodes>(query, null, token);
            return result.Select(e => e.Item3);
        }

        public static async T.Task<GroupWithNodes> GetNodeGroupAsync(this CloudUtilities utilities, int groupId, CancellationToken token)
        {
            var table = await utilities.GetOrCreateNodesTableAsync(token);
            var result = await table.RetrieveAsync<GroupWithNodes>(utilities.GroupsPartitionKey, utilities.GetGroupKey(groupId), token);
            return result;
        }
    }
}
