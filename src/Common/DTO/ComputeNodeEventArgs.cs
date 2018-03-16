namespace Microsoft.HpcAcm.Common.Dto
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;

    #region Compute Node Call Arguments

    /// <summary>
    /// Base class for arguments to delegates that report compute node events to the scheduler
    /// </summary>
    public class ComputeNodeEventArgs : EventArgs
    {
        public ComputeNodeEventArgs(string nodeName)
        {
            this.NodeName = nodeName;
        }

        public string NodeName { get; set; }
    }

    public enum TaskState
    {
        Dispatching,
        Running,
        Finished,
    }

    /// <summary>
    /// Argument for a delegate that reports that a compute node finished on the scheduler
    /// </summary>
    public class ComputeNodeTaskCompletionEventArgs : ComputeNodeEventArgs
    {
        public ComputeNodeTaskCompletionEventArgs(string nodeName, int jobId, ComputeClusterTaskInformation taskInfo)
            : base(nodeName)
        {
            this.JobId = jobId;
            this.TaskInfo = taskInfo;
        }
        public int JobId { get; set; }

        public TaskState State { get; set; }
        public ComputeClusterTaskInformation TaskInfo { get; set; }
    }

    #endregion
}
