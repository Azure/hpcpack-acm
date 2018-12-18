namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using T = System.Threading.Tasks;

    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private readonly ILogger logger;
        private readonly TaskMonitor monitor;
        private readonly CloudUtilities utilities;
        private readonly NodeSynchronizer synchronizer;
        private readonly NodeRegister register;

        public CallbackController(ILogger logger, TaskMonitor monitor, CloudUtilities utilities, NodeSynchronizer synchronizer, NodeRegister register)
        {
            this.logger = logger;
            this.monitor = monitor;
            this.utilities = utilities;
            this.synchronizer = synchronizer;
            this.register = register;
        }

        [HttpPost("computenodereported")]
        public async T.Task<int> ComputeNodeReportedAsync([FromBody] ComputeClusterNodeInformation nodeInfo, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var nodeName = nodeInfo?.Name?.ToLowerInvariant();

                this.logger.Information("ComputeNodeReported. NodeName {0}, JobCount {1}", nodeName, nodeInfo?.Jobs?.Count);

                var nodeTable = this.utilities.GetNodesTable();

                var result = await nodeTable.InsertOrReplaceAsync(
                    this.utilities.NodesPartitionKey,
                    this.utilities.GetHeartbeatKey(nodeName),
                    nodeInfo,
                    token);

                using (HttpResponseMessage r = new HttpResponseMessage((HttpStatusCode)result.HttpStatusCode))
                {
                    r.EnsureSuccessStatusCode();
                }

                await this.synchronizer.Sync(nodeInfo, token);

                // 30 s 
                return this.utilities.Option.HeartbeatIntervalSeconds * 1000;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "ComputeNodeReported. NodeName {0}, JobCount {1}", nodeInfo?.Name, nodeInfo?.Jobs?.Count);
            }

            return this.utilities.Option.RetryOnFailureSeconds * 1000;
        }

        [HttpPost("taskcompleted")]
        public T.Task<NextOperation> TaskCompletedAsync([FromBody] ComputeNodeTaskCompletionEventArgs taskInfo, CancellationToken token)
        {
            // TODO: move task key to url
            var taskKey = this.utilities.GetTaskKey(taskInfo.JobId, taskInfo.TaskInfo.TaskId, taskInfo.TaskInfo.TaskRequeueCount ?? 0);

            try
            {
                this.logger.Information("TaskCompleted. NodeName {0}, TaskKey {1} ExitCode {2} TaskMessage {3}",
                    taskInfo.NodeName,
                    taskKey,
                    taskInfo.TaskInfo.ExitCode,
                    taskInfo.TaskInfo.Message);

                this.monitor.CompleteTask(taskInfo.JobId, taskKey, taskInfo);

                return T.Task.FromResult(NextOperation.CancelTask);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Linux TaskCompleted. NodeName {0}, TaskId {1} ExitCode {2} TaskMessage {3}",
                    taskInfo.NodeName,
                    taskInfo.TaskInfo.TaskId,
                    taskInfo.TaskInfo.ExitCode,
                    taskInfo.TaskInfo.Message);

                this.monitor.FailTask(taskInfo.JobId, taskKey, ex);

                return T.Task.FromResult(NextOperation.CancelJob);
            }
        }

        [HttpPost("registerrequested")]
        public async T.Task<int> RegisterRequestedAsync([FromBody] ComputeClusterRegistrationInformation registerInfo, CancellationToken token)
        {
            try
            {
                await this.register.RegisterNodeAsync(registerInfo, token);

                // 5 minutes
                return this.utilities.Option.RegistrationIntervalSeconds * 1000;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "RegisterRequested. NodeName {0}, Distro {1}",
                    registerInfo.NodeName, registerInfo.DistroInfo);
            }

            return this.utilities.Option.RetryOnFailureSeconds * 1000;
        }
    }
}
