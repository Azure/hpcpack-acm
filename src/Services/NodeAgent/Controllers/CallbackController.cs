namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    [Route("api/[controller]")]
    public class CallbackController : Controller
    {
        private ILogger logger;
        private TaskMonitor monitor;
        private CloudUtilities utilities;
        public CallbackController(ILogger<CallbackController> logger, TaskMonitor monitor, CloudUtilities utilities)
        {
            this.logger = logger;
            this.monitor = monitor;
            this.utilities = utilities;
        }

        [HttpPost("[action]")]
        public int ComputeNodeReported([FromBody] ComputeClusterNodeInformation nodeInfo)
        {
            try
            {
                var arg = new ComputeNodeInfoEventArg(nodeInfo.Name, nodeInfo);
                this.logger.LogInformation("Linux ComputeNodeReported. NodeName {0}, JobCount {1}", nodeInfo.Name, nodeInfo.Jobs.Count);

                // TODO: handle heartbeat;
                return 30000;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Linux ComputeNodeReported. NodeName {0}, JobCount {1}", nodeInfo.Name, nodeInfo.Jobs.Count);
            }

            return 5000;
        }

        [HttpPost("[action]")]
        public NextOperation TaskCompleted([FromBody] ComputeNodeTaskCompletionEventArg taskInfo)
        {
            var taskKey = this.utilities.GetTaskKey(taskInfo.JobId, taskInfo.TaskInfo.TaskId, taskInfo.TaskInfo.TaskRequeueCount ?? 0);

            try
            {
                this.logger.LogInformation("Linux TaskCompleted. NodeName {0}, TaskKey {1} ExitCode {2} TaskMessage {3}",
                    taskInfo.NodeName,
                    taskKey,
                    taskInfo.TaskInfo.ExitCode,
                    taskInfo.TaskInfo.Message);

                this.monitor.CompleteTask(taskKey, taskInfo);

                return NextOperation.CancelTask;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Linux TaskCompleted. NodeName {0}, TaskId {1} ExitCode {2} TaskMessage {3}",
                    taskInfo.NodeName,
                    taskInfo.TaskInfo.TaskId,
                    taskInfo.TaskInfo.ExitCode,
                    taskInfo.TaskInfo.Message);
                this.monitor.FailTask(taskKey, ex);
                return NextOperation.CancelJob;
            }
        }

        [HttpPost("[action]")]
        public int RegisterRequested([FromBody] ComputeClusterRegistrationInformation registerInfo)
        {
            try
            {
                this.logger.LogInformation("Linux RegisterRequested. NodeName {0}, Distro {1} ", registerInfo.NodeName, registerInfo.DistroInfo);
                // TODO handle register

                return -1;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Linux RegisterRequested. NodeName {0}, Distro {1}",
                    registerInfo.NodeName, registerInfo.DistroInfo);
            }

            return 5000;
        }
    }
}
