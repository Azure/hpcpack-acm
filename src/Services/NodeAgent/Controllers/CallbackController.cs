namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly ILogger logger;
        private readonly TaskMonitor monitor;
        private readonly CloudUtilities utilities;

        public CallbackController(ILogger<CallbackController> logger, TaskMonitor monitor, CloudUtilities utilities)
        {
            this.logger = logger;
            this.monitor = monitor;
            this.utilities = utilities;
        }

        [HttpPost("computenodereported")]
        public async Task<int> ComputeNodeReportedAsync([FromBody] ComputeClusterNodeInformation nodeInfo, CancellationToken token)
        {
            try
            {
                var nodeName = nodeInfo.Name.ToLowerInvariant();

                this.logger.LogInformation("ComputeNodeReported. NodeName {0}, JobCount {1}", nodeName, nodeInfo.Jobs.Count);

                var nodeTable = this.utilities.GetNodesTable();

                var jsonTableEntity = new JsonTableEntity(
                    this.utilities.NodesPartitionKey,
                    this.utilities.GetHeartbeatKey(nodeName),
                    (nodeInfo, DateTime.UtcNow));

                var result = await nodeTable.ExecuteAsync(TableOperation.InsertOrReplace(jsonTableEntity), null, null, token);

                using (HttpResponseMessage r = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode))
                {
                    r.EnsureSuccessStatusCode();
                }

                // 30 s 
                return this.utilities.Option.HeartbeatIntervalSeconds * 1000;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "ComputeNodeReported. NodeName {0}, JobCount {1}", nodeInfo.Name, nodeInfo.Jobs.Count);
            }

            return this.utilities.Option.RetryOnFailureSeconds * 1000;
        }

        [HttpPost("taskcompleted")]
        public Task<NextOperation> TaskCompletedAsync([FromBody] ComputeNodeTaskCompletionEventArgs taskInfo, CancellationToken token)
        {
            // TODO: move task key to url
            var taskKey = this.utilities.GetTaskKey(taskInfo.JobId, taskInfo.TaskInfo.TaskId, taskInfo.TaskInfo.TaskRequeueCount ?? 0);

            try
            {
                this.logger.LogInformation("TaskCompleted. NodeName {0}, TaskKey {1} ExitCode {2} TaskMessage {3}",
                    taskInfo.NodeName,
                    taskKey,
                    taskInfo.TaskInfo.ExitCode,
                    taskInfo.TaskInfo.Message);

                this.monitor.CompleteTask(taskKey, taskInfo);

                return Task.FromResult(NextOperation.CancelTask);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Linux TaskCompleted. NodeName {0}, TaskId {1} ExitCode {2} TaskMessage {3}",
                    taskInfo.NodeName,
                    taskInfo.TaskInfo.TaskId,
                    taskInfo.TaskInfo.ExitCode,
                    taskInfo.TaskInfo.Message);

                this.monitor.FailTask(taskKey, ex);

                return Task.FromResult(NextOperation.CancelJob);
            }
        }

        [HttpPost("registerrequested")]
        public async Task<int> RegisterRequestedAsync([FromBody] ComputeClusterRegistrationInformation registerInfo, CancellationToken token)
        {
            try
            {
                var nodeName = registerInfo.NodeName.ToLowerInvariant();
                this.logger.LogInformation("RegisterRequested, NodeName {0}, Distro {1} ", nodeName, registerInfo.DistroInfo);
                var nodeTable = this.utilities.GetNodesTable();

                var jsonTableEntity = new JsonTableEntity(this.utilities.NodesPartitionKey, this.utilities.GetRegistrationKey(nodeName), registerInfo);
                var result = await nodeTable.ExecuteAsync(TableOperation.InsertOrReplace(jsonTableEntity), null, null, token);

                using (HttpResponseMessage r = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode))
                {
                    r.EnsureSuccessStatusCode();
                }

                // 5 minutes
                return this.utilities.Option.RegistrationIntervalSeconds * 1000;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "RegisterRequested. NodeName {0}, Distro {1}",
                    registerInfo.NodeName, registerInfo.DistroInfo);
            }

            return this.utilities.Option.RetryOnFailureSeconds * 1000;
        }
    }
}
