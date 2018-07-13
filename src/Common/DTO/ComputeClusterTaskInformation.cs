namespace Microsoft.HpcAcm.Common.Dto
{
    using System;

    public class ComputeClusterTaskInformation
    {
        public string CommandLine { get; set; }
        public string NodeName { get; set; }
        public string ResultKey { get; set; }
        public int JobId { get; set; }
        public string FilteredResult { get; set; }
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
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
    }
}
