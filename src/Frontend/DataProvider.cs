namespace Microsoft.HpcAcm.Frontend
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class DataProvider
    {
        public const int MaxPageSize = 8192;
        private readonly ILogger logger;
        private readonly CloudUtilities utilities;
        private readonly CloudTable jobsTable;
        private readonly CloudTable nodesTable;

        public DataProvider(ILogger<DataProvider> logger, CloudUtilities cloudEntities)
        {
            this.logger = logger;
            this.utilities = cloudEntities;
            this.jobsTable = this.utilities.GetJobsTable();
            this.nodesTable = this.utilities.GetNodesTable();
        }

        private async T.Task<object> GetDashboardDataAsync(string partitionName, CancellationToken token)
        {
            var dashboardTable = await this.utilities.GetOrCreateDashboardTableAsync(token);
            var partitionKey = this.utilities.GetDashboardPartitionKey(partitionName);
            var entryKey = this.utilities.GetDashboardEntryKey();
            var item = await dashboardTable.RetrieveJsonTableEntityAsync(partitionKey, entryKey, token);
            return new { LastUpdated = item.Timestamp, Data = item.GetObject<DashboardItem>()?.TotalStatistics, };
        }

        public T.Task<object> GetDashboardNodesAsync(CancellationToken token)
        {
            return this.GetDashboardDataAsync("node", token);
        }

        public T.Task<object> GetDashboardDiagnosticsAsync(CancellationToken token)
        {
            return this.GetDashboardDataAsync(JobType.Diagnostics.ToString(), token);
        }

        public T.Task<object> GetDashboardClusrunAsync(CancellationToken token)
        {
            return this.GetDashboardDataAsync(JobType.ClusRun.ToString(), token);
        }

        public async T.Task<IActionResult> GetOutputRawAsync(JobType type, string taskResultKey, CancellationToken token)
        {
            var blob = this.utilities.GetJobOutputBlob(type, taskResultKey);

            if (!await blob.ExistsAsync(null, null, token))
            {
                return new NotFoundResult();
            }

            if (this.utilities.IsSharedKeyAccount)
            {
                var sasToken = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1.0),
                });

                return new RedirectResult(blob.Uri + sasToken);
            }
            else
            {
                await blob.FetchAttributesAsync(null, null, null, token);
                var stream = await blob.OpenReadAsync(null, null, null, token);
                FileStreamResult r = new FileStreamResult(stream, blob.Properties.ContentType);
                return r;
            }
        }

        public async T.Task<TaskOutputPage> GetOutputPageAsync(JobType type, string taskResultKey, int pageSize, long offset, CancellationToken token)
        {
            if (pageSize <= 0) pageSize = MaxPageSize;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            var result = new TaskOutputPage() { Offset = offset, Size = 0 };

            var blob = this.utilities.GetJobOutputBlob(type, taskResultKey);

            if (!await blob.ExistsAsync(null, null, token))
            {
                return result;
            }

            await blob.FetchAttributesAsync();
            var blobLength = blob.Properties.Length;
            if (blobLength == 0) { return result; }

            if (offset < 0)
            {
                offset += blobLength;
                if (offset < 0) { offset = 0; }
            }

            result.Offset = offset;

            if (blobLength <= offset)
            {
                return result;
            }

            using (MemoryStream stream = new MemoryStream(pageSize))
            {
                await blob.DownloadRangeToStreamAsync(stream, offset, pageSize, null, null, null, token);
                stream.Seek(0, SeekOrigin.Begin);
                StreamReader sr = new StreamReader(stream, Encoding.UTF8);
                result.Content = await sr.ReadToEndAsync();
                result.Size = stream.Position;
            }

            return result;
        }


        public T.Task<IEnumerable<Node>> GetNodesAsync(
            string lastId,
            int count,
            CancellationToken token)
        {
            return this.utilities.GetNodesAsync(lastId, this.utilities.MaxString, count, token);
        }

        public async T.Task<Node> GetNodeAsync(string id, CancellationToken token)
        {
            id = id.ToLowerInvariant();
            var registrationKey = this.utilities.GetRegistrationKey(id);

            var registerInfo = await this.nodesTable.RetrieveAsync<ComputeClusterRegistrationInformation>(this.utilities.NodesPartitionKey, registrationKey, token);

            if (registerInfo == null) return null;
            var heartbeatKey = this.utilities.GetHeartbeatKey(id);
            var nodeInfo = await this.nodesTable.RetrieveJsonTableEntityAsync(this.utilities.NodesPartitionKey, heartbeatKey, token);

            var node = new Node() { NodeRegistrationInfo = registerInfo, Name = id, };
            if (nodeInfo != null && nodeInfo.Timestamp.AddSeconds(this.utilities.Option.MaxMissedHeartbeats * this.utilities.Option.HeartbeatIntervalSeconds) > DateTimeOffset.UtcNow)
            {
                node.Health = NodeHealth.OK;
                node.RunningJobCount = nodeInfo.GetObject<ComputeClusterNodeInformation>().Jobs.Count;
                node.EventCount = 5;
            }
            else
            {
                node.Health = NodeHealth.Error;
            }

            node.State = NodeState.Online;

            return node;
        }

        public async T.Task<IEnumerable<Job>> GetNodeJobInfoAsync(string id, CancellationToken token)
        {
            id = id.ToLowerInvariant();
            var heartbeatKey = this.utilities.GetHeartbeatKey(id);
            var nodeInfo = await this.nodesTable.RetrieveAsync<ComputeClusterNodeInformation>(this.utilities.NodesPartitionKey, heartbeatKey, token);

            if (nodeInfo == null) return null;

            var jobs = await T.Task.WhenAll(nodeInfo.Jobs.Select(async j =>
                await this.jobsTable.RetrieveAsync<Job>(this.utilities.GetJobPartitionKey(JobType.Diagnostics, j.JobId), this.utilities.JobEntryKey, token)
                    ?? await this.jobsTable.RetrieveAsync<Job>(this.utilities.GetJobPartitionKey(JobType.Diagnostics, j.JobId), this.utilities.JobEntryKey, token)));

            return jobs.Where(j => j != null);
        }

        public async T.Task<object> GetNodeMetadataAsync(string id, CancellationToken token)
        {
            var key = this.utilities.GetMetadataKey();
            return await this.nodesTable.RetrieveAsync<object>(this.utilities.GetNodePartitionKey(id), key, token);
        }

        public async T.Task<object> GetNodeScheduledEventsAsync(string id, CancellationToken token)
        {
            var key = this.utilities.GetScheduledEventsKey();
            return await this.nodesTable.RetrieveAsync<object>(this.utilities.GetNodePartitionKey(id), key, token);
        }

        public async T.Task<MetricHistory> GetNodeMetricHistoryAsync(string id, CancellationToken token)
        {
            var metricsKey = this.utilities.GetMinuteHistoryKey();
            return await this.nodesTable.RetrieveAsync<MetricHistory>(this.utilities.GetNodePartitionKey(id), metricsKey, token);
        }

        public T.Task<IEnumerable<Event>> GetNodeEventsAsync(string id, CancellationToken token)
        {
            return T.Task.FromResult<IEnumerable<Event>>(new Event[] {
                new Event() { Content = "Dummy node event.", Source = EventSource.Node, Time = DateTimeOffset.UtcNow, Type = EventType.Information },
                new Event() { Content = "Dummy node event 2.", Source = EventSource.Node, Time = DateTimeOffset.UtcNow, Type = EventType.Warning },
            });
        }

        public async T.Task<IEnumerable<DiagnosticsTest>> GetDiagnosticsTestsAsync(CancellationToken token)
        {
            var partitionString = this.utilities.GetPartitionKeyRangeString(
                this.utilities.GetDiagPartitionKey(this.utilities.MinString),
                this.utilities.GetDiagPartitionKey(this.utilities.MaxString));

            var testsResult = (await this.jobsTable.QueryAsync<DiagnosticsTest>(partitionString, null, token)).ToList();

            testsResult.ForEach(tr => { tr.Item3.Category = this.utilities.GetDiagCategoryName(tr.Item1); tr.Item3.Name = tr.Item2; });

            return testsResult.Select(tr => tr.Item3);
        }

        public T.Task<IEnumerable<Job>> GetJobsAsync(
            int lastId,
            int count = 100,
            JobType type = JobType.ClusRun,
            bool reverse = false,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} jobs called, lastId {id}, jobCount {count}", type, lastId, count);
            return this.utilities.GetJobsAsync(lastId, count: count, type: type, reverse: reverse, token: token);
        }

        public async T.Task<Job> GetJobAsync(
            int jobId,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} job called. getting job {id}", type, jobId);

            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var rowKey = utilities.JobEntryKey;

            var jsonTableEntity = await this.jobsTable.RetrieveJsonTableEntityAsync(jobPartitionKey, rowKey, token);
            var job = jsonTableEntity?.GetObject<Job>();
            if (job != null) job.UpdatedAt = jsonTableEntity.Timestamp;

            return job;
        }

        public async T.Task<object> GetJobAggregationResultAsync(
            int jobId,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            var aggregationResultBlob = this.utilities.GetJobOutputBlob(type, this.utilities.GetJobAggregationResultKey(jobId));
            if (await aggregationResultBlob.ExistsAsync(null, null, token))
            {
                return await aggregationResultBlob.DownloadTextAsync(Encoding.UTF8, null, null, null, token);
            }
            else
            {
                return null;
            }
        }

        public async T.Task<IEnumerable<Event>> GetJobEventsAsync(
            int jobId,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} job event called. getting job {id}", type, jobId);

            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var rowKey = utilities.JobEntryKey;

            var j = await this.jobsTable.RetrieveAsync<Job>(jobPartitionKey, rowKey, token);
            return j.Events;
        }

        public async T.Task<IEnumerable<Task>> GetTasksAsync(
            int jobId,
            int requeueCount,
            int lastTaskId,
            int count = 100,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} tasks called. getting job {id}", type, jobId);

            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var partitionQuery = this.utilities.GetPartitionQueryString(jobPartitionKey);

            var rowKeyRangeQuery = this.utilities.GetRowKeyRangeString(
                this.utilities.GetTaskKey(jobId, lastTaskId, requeueCount),
                this.utilities.GetTaskKey(jobId, int.MaxValue, requeueCount));

            var q = TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery);
            var tasks = await this.jobsTable.QueryAsync<Task>(q, count, token);
            return tasks.Where(r => r.Item3.CustomizedData != Task.EndTaskMark).Select(r => r.Item3);
        }

        public async T.Task<Task> GetTaskAsync(
            int jobId,
            int requeueCount,
            int id,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} task called. getting job {id}", type, jobId);

            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var taskKey = this.utilities.GetTaskKey(jobId, id, requeueCount);

            return await this.jobsTable.RetrieveAsync<Task>(jobPartitionKey, taskKey, token);
        }

        public async T.Task<ComputeClusterTaskInformation> GetTaskResultAsync(
            int jobId,
            int requeueCount,
            int id,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var taskResultKey = this.utilities.GetTaskResultKey(jobId, id, requeueCount);
            return await this.jobsTable.RetrieveAsync<ComputeClusterTaskInformation>(jobPartitionKey, taskResultKey, token);
        }

        public async T.Task<IActionResult> PatchJobAsync(Job job, CancellationToken token)
        {
            this.logger.LogInformation("Patch job called for job {0} {1}", job.Type, job.Id);

            JobState state = JobState.Finished;

            if (!await this.utilities.UpdateJobAsync(job.Type, job.Id, j =>
            {
                state = j.State = (j.State == JobState.Queued || j.State == JobState.Running) ? JobState.Canceling : j.State;
            }, token))
            {
                return new NotFoundObjectResult($"{job.Type} job {job.Id} was not found.");
            }

            if (state == JobState.Canceling)
            {
                var jobEventQueue = this.utilities.GetJobEventQueue();
                var jobMsg = new JobEventMessage() { Id = job.Id, Type = job.Type, EventVerb = "cancel" };
                await jobEventQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(jobMsg)), null, null, null, null, token);
                this.logger.LogInformation("Create job dispatch message success.");
                return new OkObjectResult($"{job.Type} job {job.Id} is being canceled.");
            }
            else
            {
                return new BadRequestObjectResult($"Cannot cancel {job.Type} job {job.Id} because it is in {state} state.");
            }
        }

        public async T.Task<Job> CreateJobAsync(Job job, CancellationToken token)
        {
            this.logger.LogInformation("New job called. creating job");
            var jobTable = this.utilities.GetJobsTable();

            job.Id = await this.utilities.GetNextId("Jobs", "Jobs", token);
            this.logger.LogInformation("generated new job id {0}", job.Id);
            var rowKey = utilities.JobEntryKey;

            job.CreatedAt = DateTimeOffset.UtcNow;

            var partitionName = utilities.GetJobPartitionKey(job.Type, job.Id);
            var result = await jobTable.InsertOrReplaceAsync(partitionName, rowKey, job, token);
            this.logger.LogInformation("create job result {0}", result);

            partitionName = utilities.GetJobPartitionKey(job.Type, job.Id, true);
            result = await jobTable.InsertOrReplaceAsync(partitionName, rowKey, job, token);
            this.logger.LogInformation("create job result {0}", result);

            this.logger.LogInformation("Creating job dispatch message");
            var jobEventQueue = this.utilities.GetJobEventQueue();

            var jobMsg = new JobEventMessage() { Id = job.Id, Type = job.Type, EventVerb = "dispatch" };
            await jobEventQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(jobMsg)), null, null, null, null, token);
            this.logger.LogInformation("Create job dispatch message success.");

            return job;
        }
    }
}
