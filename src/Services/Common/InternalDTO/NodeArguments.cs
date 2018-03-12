namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;

    #region CallBack Method Arguments

    /// <summary>
    /// Base class for arguments to node communicator methods.
    /// These methods are used by the scheduler
    /// to perform operations on the compute nodes.
    /// These arguments are also used in the callback at the end of the the operation.
    /// The callback at the end of the operation informs the scheduler that the
    /// operation on the compute node has completed.
    /// </summary>
    public class NodeCommunicatorCallBackArg
    {
        IEnumerable<int> resIds;

        public NodeCommunicatorCallBackArg(IEnumerable<int> resIds)
        {
            this.resIds = resIds;
        }

        public IEnumerable<int> ResIds
        {
            get { return resIds; }
        }


    }

    //delegate for callbacks methods used by the node communicator
    // to report completion of operations initiated by the scheduler
    public delegate void NodeCommunicatorCallBack<ArgT>(string nodeName, ArgT arg, Exception exception) where ArgT : NodeCommunicatorCallBackArg;

    /// <summary>
    /// Argument for the StartJob operation issued by the scheduler
    /// </summary>
    public class StartJobArg : NodeCommunicatorCallBackArg
    {
        int jobId;

        public StartJobArg(IEnumerable<int> resIds, int jobId)
            : base(resIds)
        {
            this.jobId = jobId;
        }

        public int JobId
        {
            get { return jobId; }
        }

        public override string ToString()
        {
            return "Start job " + jobId;
        }
    }

    /// <summary>
    /// Argument for the EndJob operation issued by the scheduler
    /// </summary>
    public class EndJobArg : NodeCommunicatorCallBackArg
    {
        int jobId;
        ComputeClusterJobInformation jobInfo;

        public EndJobArg(IEnumerable<int> resIds, int jobId)
            : base(resIds)
        {
            this.jobId = jobId;
        }

        public int JobId
        {
            get { return jobId; }
        }

        public ComputeClusterJobInformation JobInfo
        {
            get { return jobInfo; }
            set { jobInfo = value; }
        }

        public override string ToString()
        {
            return "End job " + jobId;
        }
    }

    /// <summary>
    /// Argument for the StartTask operation issued by the scheduler
    /// </summary>
    public class StartTaskArg : NodeCommunicatorCallBackArg
    {
        int jobId;
        int taskId;

        public StartTaskArg(IEnumerable<int> resIds, int jobId, int taskId)
            : base(resIds)
        {
            this.jobId = jobId;
            this.taskId = taskId;
        }

        public int JobId
        {
            get { return jobId; }
        }

        public int TaskId
        {
            get { return taskId; }
        }

        public override string ToString()
        {
            return "Start task " + jobId + "." + taskId;
        }
    }

    /// <summary>
    /// Argument for the EndTask operation issued by the scheduler
    /// It also includes the taskinfo object that is used by the callback
    /// to report the task's state on the compute node at the completion of the
    /// EndTask operation
    /// </summary>
    public class EndTaskArg : NodeCommunicatorCallBackArg
    {
        int jobId;
        int taskId;
        int gracePeriod;
        ComputeClusterTaskInformation taskInfo;

        public EndTaskArg(IEnumerable<int> resIds, int jobId, int taskId)
            : base(resIds)
        {
            this.jobId = jobId;
            this.taskId = taskId;
            this.gracePeriod = 0;
        }

        public EndTaskArg(IEnumerable<int> resIds, int jobId, int taskId, int gracePeriod)
            : base(resIds)
        {
            this.jobId = jobId;
            this.taskId = taskId;
            this.gracePeriod = gracePeriod;
        }

        public int JobId
        {
            get { return jobId; }
        }

        public int TaskId
        {
            get { return taskId; }
        }

        public int TaskCancelGracePeriod
        {
            get { return gracePeriod; }
        }

        public ComputeClusterTaskInformation TaskInfo
        {
            get { return taskInfo; }
            set { taskInfo = value; }
        }

        public override string ToString()
        {
            return "End task " + jobId + "." + taskId;
        }
    }

    /// <summary>
    /// /// <summary>
    /// Argument for the StartJobAndTask operation issued by the scheduler
    /// </summary>
    /// </summary>
    public class StartJobAndTaskArg : NodeCommunicatorCallBackArg
    {
        int jobId;
        int taskId;

        public StartJobAndTaskArg(IEnumerable<int> resIds, int jobId, int taskId)
            : base(resIds)
        {
            this.jobId = jobId;
            this.taskId = taskId;
        }

        public int JobId
        {
            get { return jobId; }
        }

        public int TaskId
        {
            get { return taskId; }
        }

        public override string ToString()
        {
            return "Start job and task " + jobId + "." + taskId;
        }
    }

    public class PeekTaskOutputArg : NodeCommunicatorCallBackArg
    {
        public PeekTaskOutputArg(int jobId, int taskId) : base(null)
        {
            this.JobId = jobId;
            this.TaskId = taskId;
        }

        public int JobId { get; }

        public int TaskId { get; }

        public string Output
        {
            get;
            set;
        }

        public override string ToString()
        {
            return "Peek output of task " + JobId + "." + TaskId;
        }
    }
    #endregion

    #region Compute Node Call Arguments

    /// <summary>
    /// Base class for arguments to delegates that report compute node events to the scheduler
    /// </summary>
    public class ComputeNodeEventArg : EventArgs
    {
        string _nodeName;
        public ComputeNodeEventArg(string nodeName)
            : base()
        {
            _nodeName = nodeName;
        }


        public string NodeName
        {
            get { return _nodeName; }
        }
    }
    /// <summary>
    /// Argument for a delegate that reports that a compute node finished on the scheduler
    /// </summary>
    public class ComputeNodeTaskCompletionEventArg : ComputeNodeEventArg
    {
        int _jobId;
        ComputeClusterTaskInformation _taskInfo;

        public ComputeNodeTaskCompletionEventArg(string nodeName, int jobId, ComputeClusterTaskInformation taskInfo)
            : base(nodeName)
        {
            _jobId = jobId;
            _taskInfo = taskInfo;
        }
        public int JobId
        {
            get { return _jobId; }
        }
        public ComputeClusterTaskInformation TaskInfo
        {
            get { return _taskInfo; }
        }
    }
    #endregion


    #region delegates from the scheduler
    public delegate NextOperation TaskCompletionDelegate(ComputeNodeTaskCompletionEventArg arg);
    public delegate void SetReachableOnNodeDelegate(string nodeName, bool isReachable);
    public delegate bool IsNodeValidDelegate(string nodeName, IIdentity identity);
    public delegate void GetClusterConfigurationDelegate(IEnumerable<string> configSettings, out List<string> configValues);
    #endregion

    /// <summary>
    /// This is the interface that must be implemented by any communication mechanism for communicating between the scheduler
    /// and compute nodes
    /// This interface is designed to be used without reference to the hpc.scheduler.store and hpcschedulercore dlls.
    /// </summary>
    public interface INodeCommunicator
    {
        /// <summary>
        /// Initialize the communication mechanism
        /// </summary>
        /// <returns></returns>
        bool Initialize();

        /// <summary>
        /// Start the communication mechanism to allow the scheduler to communicate
        /// with compute nodes
        /// </summary>
        /// <returns></returns>
        bool Start();

        /// <summary>
        /// Shutdown this communication mechanism
        /// </summary>
        /// <returns></returns>
        bool Stop();

        #region Operations initiated by the scheduler
        /// <summary>
        /// Start this task which is the first for its job on a node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the jobid and task id as well as the resources to use on the node </param>
        /// <param name="userName">Username to start the task</param>
        /// <param name="password">Password for the user </param>
        /// <param name="startInfo">Information to setup the process's execution environment</param>
        /// <param name="callback">The callback to invoke once the operation (not the task) has finished on the compute node</param>
        void StartJobAndTask(string nodeName, StartJobAndTaskArg arg, string userName, string password, ProcessStartInfo startInfo, NodeCommunicatorCallBack<StartJobAndTaskArg> callback);

        /// <summary>
        /// Start this task which is the first for its job on a node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the jobid and task id as well as the resources to use on the node </param>
        /// <param name="userName">Username to start the task</param>
        /// <param name="password">Password for the user </param>
        /// <param name="extendedData">extended data for the user </param>
        /// <param name="startInfo">Information to setup the process's execution environment</param>
        /// <param name="callback">The callback to invoke once the operation (not the task) has finished on the compute node</param>
        void StartJobAndTaskExtendedData(string nodeName, StartJobAndTaskArg arg, string userName, string password, string extendedData, ProcessStartInfo startInfo, NodeCommunicatorCallBack<StartJobAndTaskArg> callback);

        /// <summary>
        /// Start this task which is the first for its job on a node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the jobid and task id as well as the resources to use on the node </param>
        /// <param name="userName">Username to start the task</param>
        /// <param name="password">Password for the user </param>
        /// <param name="certificate">SoftCard Certificate for the user </param>
        /// <param name="startInfo">Information to setup the process's execution environment</param>
        /// <param name="callback">The callback to invoke once the operation (not the task) has finished on the compute node</param>
        void StartJobAndTaskSoftCardCred(string nodeName, StartJobAndTaskArg arg, string userName, string password, byte[] certificate, ProcessStartInfo startInfo, NodeCommunicatorCallBack<StartJobAndTaskArg> callback);


        /// <summary>
        /// Start the task which is not the first for this job on this node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the jobid and task id as well as the resources to use on the node  </param>
        /// <param name="startInfo">Information to setup the process's execution environment</param>
        /// <param name="callback">The callback to invoke once the operation has finished on the compute node</param>
        void StartTask(string nodeName, StartTaskArg arg, ProcessStartInfo startInfo, NodeCommunicatorCallBack<StartTaskArg> callback);

        /// <summary>
        /// End a job on the compute node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the job id</param>
        /// <param name="callback">The callback to invoke once the operation has finished on the compute node</param>
        void EndJob(string nodeName, EndJobArg arg, NodeCommunicatorCallBack<EndJobArg> callback);

        /// <summary>
        /// End a task on the compute node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the job id and task id </param>
        /// <param name="callback">The callback to invoke once the operation has finished on the compute node
        /// When the callback is invoked, the argument to the callback should include information about the task from the compute node
        /// </param>
        void EndTask(string nodeName, EndTaskArg arg, NodeCommunicatorCallBack<EndTaskArg> callback);

        /// <summary>
        /// This method notifies the communicator once the scheduler decides to mark the node as unreachable.
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="reachable"></param>
        void OnNodeStatusChange(string nodeName, bool reachable);

        /// <summary>
        /// Peek the output of a task on the compute node
        /// The method should be asynchronous and invoke the callback on completion
        /// </summary>
        /// <param name="nodeName">Name of the compute node on which to perform the operation</param>
        /// <param name="arg">Contains the job id and task id </param>
        /// <param name="callback">The callback to invoke once the operation has finished on the compute node
        /// When the callback is invoked, the argument to the callback should include the output of the task from the compute node
        /// </param>
        void PeekTaskOutput(string nodeName, PeekTaskOutputArg arg, NodeCommunicatorCallBack<PeekTaskOutputArg> callback);
        #endregion

        #region Delegates used by the communicator to report events to and exchange data with the scheduler

        /// <summary>
        /// Callback used to report the completion of a task to the scheduler
        /// </summary>
        TaskCompletionDelegate OnTaskCompletion
        {
            get;
            set;
        }

        /// <summary>
        /// Delegate used to mark a node as reachable or unreachable in the scheduler
        /// </summary>
        SetReachableOnNodeDelegate SetReachableOnNode
        {
            get;
            set;
        }

        /// <summary>
        /// Delegate used to query the scheduler if a particular node is valid
        /// </summary>
        IsNodeValidDelegate IsNodeValid
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Delegate used to query the scheduler for a list of cluster configuration parameters
        /// </summary>
        GetClusterConfigurationDelegate GetClusterConfiguration
        {
            get;
            set;
        }
    }

    public delegate IEnumerable<string> GetDeploymentIdsDelegate();
    public delegate string GetDecryptedString(byte[] dataBlob);
}
