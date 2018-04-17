namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;

    public enum JobState
    {
        Queued,
        Running,
        Finished,
        Finishing,
        Cancelling,
        Failed,
        Canceled,
    }

    public enum JobType
    {
        ClusRun,
        Diagnostics,
    }

    public class Job
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public JobState State { get; set; }
        public JobType Type { get; set; }
        public int Progress { get; set; }
        public int RequeueCount { get; set; } = 0;
        public bool FailJobOnTaskFailure { get; set; } = false;

        public DiagnosticsTest DiagnosticTest { get; set; }
        public string CommandLine { get; set; }
        public string[] TargetNodes { get; set; }
        public List<Event> Events { get; set; }
        public string AggregationResult { get; set; }
    }
}