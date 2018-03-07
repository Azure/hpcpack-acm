namespace Microsoft.HpcAcm.Services.Common
{
    using System;

    public class ComputeClusterTaskInformation
    {
        /// <summary>
        /// Default Constructor
        /// </summary>
        public ComputeClusterTaskInformation() { }

        /// <summary>
        /// Copy Constructor used to create base object from a derived class
        /// </summary>
        public ComputeClusterTaskInformation(ComputeClusterTaskInformation previousTaskInformation)
        {
            if (null == previousTaskInformation)
            {
                throw new ArgumentNullException("previousTaskInformation");
            }

            TaskId = previousTaskInformation.TaskId;
            Message = previousTaskInformation.Message;
            NumberOfProcesses = previousTaskInformation.NumberOfProcesses;
            ProcessIds = previousTaskInformation.ProcessIds;
            KernelProcessorTime = previousTaskInformation.KernelProcessorTime;
            UserProcessorTime = previousTaskInformation.UserProcessorTime;
            WorkingSet = previousTaskInformation.WorkingSet;
            PrimaryTask = previousTaskInformation.PrimaryTask;
            Exited = previousTaskInformation.Exited;
            ExitCode = previousTaskInformation.ExitCode;
            TaskRequeueCount = previousTaskInformation.TaskRequeueCount;
        }

        public int TaskId { get; set; }

        public string Message { get; set; }

        public int NumberOfProcesses { get; set; }

        public string ProcessIds { get; set; }

        public Int64 KernelProcessorTime { get; set; }

        public Int64 UserProcessorTime { get; set; }

        public int WorkingSet { get; set; }

        public bool PrimaryTask { get; set; }

        public bool Exited { get; set; }

        public int ExitCode { get; set; }

        public int? TaskRequeueCount { get; set; }
    }
}
