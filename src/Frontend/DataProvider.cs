namespace Microsoft.HpcAcm.Frontend
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
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
    using System.Threading.Tasks;

    public class DataProvider
    {
        private readonly ILogger logger;
        private readonly CloudUtilities utilities;

        public DataProvider(ILogger<DataProvider> logger, CloudUtilities cloudEntities)
        {
            this.logger = logger;
            this.utilities = cloudEntities;
        }

        public async Task<IActionResult> GetOutputRawAsync(JobType type, int jobId, string taskResultKey, CancellationToken token)
        {
            var blob = this.utilities.GetTaskOutputBlob(type.ToString().ToLowerInvariant(), jobId, taskResultKey);

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

        public async Task<TaskOutputPage> GetOutputPageAsync(JobType type, int jobId, string taskResultKey, int pageSize, long offset, CancellationToken token)
        {
            if (pageSize <= 0) pageSize = 1024;
            if (pageSize > 1024) pageSize = 1024;

            var result = new TaskOutputPage() { Offset = offset, Size = 0 };

            var blob = this.utilities.GetTaskOutputBlob(type.ToString().ToLowerInvariant(), jobId, taskResultKey);

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


        public async Task<IEnumerable<Node>> GetNodesAsync(
            string lastId,
            int count,
            CancellationToken token)
        {
            // todo: abstract range query
            var partitionQuery = this.utilities.GetPartitionQueryString(this.utilities.NodesPartitionKey);

            var lastRegistrationKey = this.utilities.GetRegistrationKey(lastId);
            var registrationEnd = this.utilities.GetMaximumRegistrationKey();

            var registrationRangeQuery = this.utilities.GetRowKeyRangeString(lastRegistrationKey, registrationEnd);

            var q = TableQuery.CombineFilters(
                partitionQuery,
                TableOperators.And,
                registrationRangeQuery);

            var nodes = this.utilities.GetNodesTable();

            var registrations = (await nodes.QueryAsync<ComputeClusterRegistrationInformation>(q, count, token)).Select(r => r.Item3);

            if (!registrations.Any())
            {
                return new Node[0];
            }

            var firstHeartbeat = this.utilities.GetHeartbeatKey(registrations.First().NodeName.ToLowerInvariant());
            var lastHeartbeat = this.utilities.GetHeartbeatKey(registrations.Last().NodeName.ToLowerInvariant());

            var heartbeatRangeQuery = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, firstHeartbeat),
                TableOperators.And,
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, lastHeartbeat));

            q = TableQuery.CombineFilters(
                partitionQuery,
                TableOperators.And,
                heartbeatRangeQuery);

            var heartbeats = (await nodes.QueryAsync<ComputeClusterNodeInformation>(q, null, token)).ToDictionary(h => h.Item3.Name.ToLowerInvariant(), h => (h.Item3, h.Item4));

            return registrations.Select(r =>
            {
                var nodeName = r.NodeName.ToLowerInvariant();
                var node = new Node() { NodeRegistrationInfo = r, Name = nodeName, };

                if (heartbeats.TryGetValue(nodeName, out (ComputeClusterNodeInformation, DateTimeOffset) n))
                {
                    if (n.Item2.AddSeconds(this.utilities.Option.MaxMissedHeartbeats * this.utilities.Option.HeartbeatIntervalSeconds) > DateTimeOffset.UtcNow)
                    {
                        node.Health = NodeHealth.OK;
                        node.State = NodeState.Online;
                        node.RunningJobCount = n.Item1.Jobs.Count;
                        // TODO: adding events
                        node.EventCount = 5;
                    }
                    else
                    {
                        node.Health = NodeHealth.Error;
                    }
                }

                return node;
            });
        }

        public async Task<IActionResult> GetNodeAsync(string id, CancellationToken token)
        {
            id = id.ToLowerInvariant();
            var registrationKey = this.utilities.GetRegistrationKey(id);

            var nodes = this.utilities.GetNodesTable();
            var result = await nodes.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(this.utilities.NodesPartitionKey, registrationKey), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                return new StatusCodeResult(result.HttpStatusCode);
            }

            ComputeClusterRegistrationInformation registerInfo = (result.Result as JsonTableEntity)?.GetObject<ComputeClusterRegistrationInformation>();

            var heartbeatKey = this.utilities.GetHeartbeatKey(id);
            result = await nodes.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(this.utilities.NodesPartitionKey, heartbeatKey), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                return new StatusCodeResult(result.HttpStatusCode);
            }

            var entity = result.Result as JsonTableEntity;
            ComputeClusterNodeInformation nodeInfo = entity?.GetObject<ComputeClusterNodeInformation>();

            var node = new Node() { NodeRegistrationInfo = registerInfo, Name = id, };
            if (entity?.Timestamp.AddSeconds(this.utilities.Option.MaxMissedHeartbeats * this.utilities.Option.HeartbeatIntervalSeconds) > DateTimeOffset.UtcNow)
            {
                node.Health = NodeHealth.OK;
                node.RunningJobCount = nodeInfo.Jobs.Count;
                node.EventCount = 5;
            }
            else
            {
                node.Health = NodeHealth.Error;
            }

            node.State = NodeState.Online;

            var nodeDetails = new NodeDetails() { NodeInfo = node, Jobs = nodeInfo?.Jobs, };

            var metricsKey = this.utilities.GetMinuteHistoryKey();
            result = await nodes.ExecuteAsync(TableOperation.Retrieve<JsonTableEntity>(this.utilities.GetNodePartitionKey(id), metricsKey), null, null, token);

            if (!result.IsSuccessfulStatusCode())
            {
                return new StatusCodeResult(result.HttpStatusCode);
            }

            var historyEntity = result.Result as JsonTableEntity;
            nodeDetails.History = historyEntity.GetObject<MetricHistory>();

            return new OkObjectResult(nodeDetails);
        }

        public async Task<IEnumerable<DiagnosticsTest>> GetDiagnosticsTestsAsync(CancellationToken token)
        {
            var jobsTable = this.utilities.GetJobsTable();

            var partitionString = this.utilities.GetPartitionKeyRangeString(
                this.utilities.GetDiagPartitionKey(this.utilities.MinString),
                this.utilities.GetDiagPartitionKey(this.utilities.MaxString));

            var testsResult = (await jobsTable.QueryAsync<DiagnosticsTest>(partitionString, null, token)).ToList();

            testsResult.ForEach(tr => { tr.Item3.Category = this.utilities.GetDiagCategoryName(tr.Item1); tr.Item3.Name = tr.Item2; });

            return testsResult.Select(tr => tr.Item3);
        }

        public async Task<IEnumerable<Job>> GetJobsAsync(
            int lastId,
            int count = 1000,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} jobs called, lastId {id}, jobCount {count}", type, lastId, count);
            var jobTable = this.utilities.GetJobsTable();

            var lowJobPartitionKey = this.utilities.GetJobPartitionKey(type, lastId);
            var highJobPartitionKey = this.utilities.GetJobPartitionKey(type, int.MaxValue);

            var partitionRange = this.utilities.GetPartitionKeyRangeString(lowJobPartitionKey, highJobPartitionKey);
            var rowKey = utilities.JobEntryKey;

            var q = TableQuery.CombineFilters(
                partitionRange,
                TableOperators.And,
                TableQuery.GenerateFilterCondition(CloudUtilities.RowKeyName, QueryComparisons.Equal, rowKey));

            var results = await jobTable.QueryAsync<Job>(q, count, token);
            return results.Select(r => r.Item3);
        }

        public async Task<IEnumerable<ComputeNodeTaskCompletionEventArgs>> GetTasksAsync(
            int jobId,
            int requeueCount,
            int lastTaskId,
            int count = 1000,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} tasks called. getting job {id}", type, jobId);
            var jobTable = this.utilities.GetJobsTable();

            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var partitionQuery = this.utilities.GetPartitionQueryString(jobPartitionKey);

            var rowKeyRangeQuery = this.utilities.GetRowKeyRangeString(
                this.utilities.GetTaskResultKey(jobId, lastTaskId, requeueCount),
                this.utilities.GetTaskResultKey(jobId, int.MaxValue, requeueCount));

            var q = TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery);
            var results = await jobTable.QueryAsync<ComputeNodeTaskCompletionEventArgs>(q, count, token);
            return results.Select(r => r.Item3);
        }

        public async Task<JobResult> GetJobAsync(
            int jobId,
            string lastNodeName,
            int nodeCount = 1000,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} job called. getting job {id}", type, jobId);
            var jobTable = this.utilities.GetJobsTable();

            var jobPartitionKey = this.utilities.GetJobPartitionKey(type, jobId);
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Retrieve<JsonTableEntity>(jobPartitionKey, rowKey),
                null, null, token);

            this.logger.LogInformation("Retrive job {0} status code {1}", jobId, result.HttpStatusCode);

            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode);
            response.EnsureSuccessStatusCode();

            if (result.Result == null)
            {
                return null;
            }

            JobResult j = ((JsonTableEntity)result.Result).GetObject<JobResult>();

            this.logger.LogInformation("Fetching job {0} output", jobId);

            var lowResultKey = this.utilities.GetNodeTaskResultKey(lastNodeName, jobId, j.RequeueCount, 0);
            var highResultKey = this.utilities.GetMaximumNodeTaskResultKey();
            var partitionQuery = this.utilities.GetPartitionQueryString(jobPartitionKey);
            var rowKeyRangeQuery = this.utilities.GetRowKeyRangeString(lowResultKey, highResultKey);

            var q = TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery);

            var taskInfos = await jobTable.QueryAsync<ComputeNodeTaskCompletionEventArgs>(q, nodeCount, token);

            j.Results = taskInfos.GroupBy(t => t.Item3.NodeName.ToLowerInvariant()).Select(g => new NodeResult()
            {
                NodeName = g.Key,
                JobId = jobId,
                Results = g.Select(e => new CommandResult()
                {
                    CommandLine = j.CommandLine,
                    NodeName = g.Key,
                    ResultKey = e.Item2,
                    TaskInfo = e.Item3.TaskInfo,
                    Test = j.DiagnosticTest,
                }).ToList(),
            }).ToList();

            return j;
        }

        public async Task<int> CreateJobAsync(Job job, CancellationToken token)
        {
            this.logger.LogInformation("New job called. creating job");
            var jobTable = this.utilities.GetJobsTable();

            job.Id = await this.utilities.GetNextId("Jobs", job.Type.ToString().ToLowerInvariant(), token);
            this.logger.LogInformation("generated new job id {0}", job.Id);

            var partitionName = utilities.GetJobPartitionKey(job.Type, job.Id);
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Insert(new JsonTableEntity(partitionName, rowKey, job)),
                null, null, token);

            this.logger.LogInformation("create job result {0}", result.HttpStatusCode);

            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode);
            response.EnsureSuccessStatusCode();

            this.logger.LogInformation("Creating job dispatch message");
            var jobEventQueue = this.utilities.GetJobEventQueue();

            var jobMsg = new JobEventMessage() { Id = job.Id, Type = job.Type, EventVerb = "dispatch" };
            await jobEventQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(jobMsg)), null, null, null, null, token);
            this.logger.LogInformation("Create job dispatch message success.");

            return job.Id;
        }
    }
}
