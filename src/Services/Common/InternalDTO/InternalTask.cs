namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class InternalTask
    {
        public const string StartTaskMark = "Start";
        public const string EndTaskMark = "End";

        public static InternalTask CreateFrom(Job job)
        {
            return new InternalTask()
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
        public List<int> ParentIds { get; set; }
        public List<int> ChildIds { get; set; }
        public HashSet<int> RemainingParentIds { get; set; }
        public string CommandLine { get; set; }
        public string WorkingDirectory { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
        public string Node { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }

        public int MaximumRuntimeSeconds { get; set; } = 300;
        public string CustomizedData { get; set; }
    }
}
