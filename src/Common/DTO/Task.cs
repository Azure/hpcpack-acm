namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public enum TaskState
    {
        Queued,
        Dispatching,
        Running,
        Finished,
        Failed,
        Canceled,
    }

    public class Task
    {
        public const string StartTaskMark = "Start";
        public const string EndTaskMark = "End";

        public static Task CreateFrom(Job job)
        {
            return new Task()
            {
                JobId = job.Id,
                RequeueCount = job.RequeueCount,
                JobType = job.Type,
                CommandLine = job.CommandLine,
            };
        }

        public int JobId { get; set; }
        public int Id { get; set; }
        public int RequeueCount { get; set; }
        public JobType JobType { get; set; }
        public TaskState State { get; set; }
        public int RemainingParentCount { get; set; }
        public string CommandLine { get; set; }
        public string Node { get; set; }
        public string CustomizedData { get; set; }
        public List<int> ChildIds { get; set; }
        public string ZippedParentIds { get; set; }
    }
}
