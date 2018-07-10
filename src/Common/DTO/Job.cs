namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;

    public enum JobState
    {
        Queued,
        Running,
        Finished,
        Finishing,
        /// <summary>
        /// A state that the job is being canceled by user, or timeout.
        /// The job won't dispatch new tasks to nodes
        /// The running tasks is being canceled.
        /// </summary>
        Canceling,
        Failed,
        /// <summary>
        /// A state that the job finished its cancel process.
        /// The job won't dispatch new tasks to nodes
        /// </summary>
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
        public string Request { get; set; }
        public string Name { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public JobState State { get; set; }
        public JobType Type { get; set; }
        public double Progress { get => 1.0 * this.CompletedTaskCount / this.TaskCount; }
        public int RequeueCount { get; set; } = 0;
        public bool FailJobOnTaskFailure { get; set; } = false;

        public int CompletedTaskCount { get; set; }
        public int TaskCount { get; set; }
        public DiagnosticsTest DiagnosticTest { get; set; }
        public string CommandLine { get; set; }
        public string[] TargetNodes { get; set; }
        public List<Event> Events { get; set; }
    }
}