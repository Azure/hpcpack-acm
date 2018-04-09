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

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    partitionQuery,
                    TableOperators.And,
                    registrationRangeQuery))
                .Take(count);

            var nodes = this.utilities.GetNodesTable();

            List<ComputeClusterRegistrationInformation> registrations = new List<ComputeClusterRegistrationInformation>();
            TableContinuationToken conToken = null;

            do
            {
                var result = await nodes.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                registrations.AddRange(
                    result.Results.Select(r => r.GetObject<ComputeClusterRegistrationInformation>()));

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

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

            q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    partitionQuery,
                    TableOperators.And,
                    heartbeatRangeQuery));

            conToken = null;

            var heartbeats = new Dictionary<string, (ComputeClusterNodeInformation, DateTimeOffset)>();

            do
            {
                var result = await nodes.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);
                foreach (var h in result.Results.Select(r => (r.GetObject<ComputeClusterNodeInformation>(), r.Timestamp)))
                {
                    heartbeats[h.Item1.Name.ToLowerInvariant()] = h;
                }

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

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

            TableContinuationToken conToken = null;
            List<DiagnosticsTest> tests = new List<DiagnosticsTest>();

            var q = new TableQuery<JsonTableEntity>().Where(partitionString);
            q.SelectColumns = new List<string>() { CloudUtilities.PartitionKeyName, CloudUtilities.RowKeyName };

            do
            {
                var result = await jobsTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                tests.AddRange(result.Results.Select(e => new DiagnosticsTest()
                {
                    Category = this.utilities.GetDiagCategoryName(e.PartitionKey),
                    Name = e.RowKey
                }));

                conToken = result.ContinuationToken;
            }
            while (conToken != null);

            return tests;
        }

        public async Task<IEnumerable<Job>> GetJobsAsync(
            int lastId,
            int count = 1000,
            JobType type = JobType.ClusRun,
            CancellationToken token = default(CancellationToken))
        {
            this.logger.LogInformation("Get {type} jobs called, lastId {id}, jobCount {count}", type, lastId, count);
            var jobTable = this.utilities.GetJobsTable();

            var lowJobPartitionKey = this.utilities.GetJobPartitionKey($"{type}", lastId);
            var highJobPartitionKey = this.utilities.GetJobPartitionKey($"{type}", int.MaxValue);

            var partitionRange = this.utilities.GetPartitionKeyRangeString(lowJobPartitionKey, highJobPartitionKey);
            var rowKey = utilities.JobEntryKey;

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(
                    partitionRange,
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition(CloudUtilities.RowKeyName, QueryComparisons.Equal, rowKey)))
                .Take(count);

            var jobs = new List<Job>(count);
            TableContinuationToken conToken = null;

            do
            {
                var queryResult = await jobTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                jobs.AddRange(queryResult.Results.Select(r => r.GetObject<Job>()));

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null);

            return jobs;
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

            var jobPartitionKey = this.utilities.GetJobPartitionKey($"{type}", jobId);
            var partitionQuery = this.utilities.GetPartitionQueryString(jobPartitionKey);

            var rowKeyRangeQuery = this.utilities.GetRowKeyRangeString(
                this.utilities.GetTaskKey(jobId, lastTaskId, requeueCount),
                this.utilities.GetTaskKey(jobId, int.MaxValue, requeueCount));

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery))
                .Take(count);

            TableContinuationToken conToken = null;

            var taskInfos = new List<ComputeNodeTaskCompletionEventArgs>(count);

            do
            {
                var queryResult = await jobTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                taskInfos.AddRange(queryResult.Results.Select(r => r.GetObject<ComputeNodeTaskCompletionEventArgs>()));

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null);

            return taskInfos;
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

            var jobPartitionKey = this.utilities.GetJobPartitionKey($"{type}", jobId);
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

            var lowResultKey = this.utilities.GetJobResultKey(lastNodeName, null);
            var highResultKey = this.utilities.GetMaximumJobResultKey();
            var partitionQuery = this.utilities.GetPartitionQueryString(jobPartitionKey);
            var rowKeyRangeQuery = this.utilities.GetRowKeyRangeString(lowResultKey, highResultKey);

            var q = new TableQuery<JsonTableEntity>()
                .Where(TableQuery.CombineFilters(partitionQuery, TableOperators.And, rowKeyRangeQuery))
                .Take(nodeCount);

            TableContinuationToken conToken = null;

            j.Results = new List<NodeResult>(nodeCount);

            var taskInfos = new List<(string, ComputeNodeTaskCompletionEventArgs)>();

            do
            {
                var queryResult = await jobTable.ExecuteQuerySegmentedAsync(q, conToken, null, null, token);

                taskInfos.AddRange(queryResult.Results.Select(r => (r.RowKey, r.GetObject<ComputeNodeTaskCompletionEventArgs>())));

                conToken = queryResult.ContinuationToken;
            }
            while (conToken != null);

            j.Results = taskInfos.GroupBy(t => t.Item2.NodeName.ToLowerInvariant()).Select(g => new NodeResult()
            {
                NodeName = g.Key,
                JobId = jobId,
                Results = g.Select(e => new CommandResult()
                {
                    CommandLine = j.CommandLine,
                    NodeName = g.Key,
                    ResultKey = e.Item1,
                    TaskInfo = e.Item2.TaskInfo,
                    Test = j.DiagnosticTest,
                }).ToList(),
            }).ToList();

            return j;
        }

        public async Task<int> CreateJobAsync(Job job, CancellationToken token)
        {
            this.logger.LogInformation("New job called. creating job");
            var jobTable = this.utilities.GetJobsTable();

            job.Id = await this.utilities.GetNextId("Jobs", $"{job.Type}", token);
            this.logger.LogInformation("generated new job id {0}", job.Id);

            var partitionName = utilities.GetJobPartitionKey($"{job.Type}", job.Id);
            var rowKey = utilities.JobEntryKey;

            var result = await jobTable.ExecuteAsync(
                TableOperation.Insert(new JsonTableEntity(partitionName, rowKey, job)),
                null, null, token);

            this.logger.LogInformation("create job result {0}", result.HttpStatusCode);

            HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode);
            response.EnsureSuccessStatusCode();

            this.logger.LogInformation("Creating job dispatch message");
            var jobDispatchQueue = this.utilities.GetJobDispatchQueue();

            var jobMsg = new JobDispatchMessage() { Id = job.Id, Type = job.Type };
            await jobDispatchQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(jobMsg)), null, null, null, null, token);
            this.logger.LogInformation("Create job dispatch message success.");

            return job.Id;
        }
    }
}
