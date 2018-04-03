namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskCompletionMessage
    {
        public int JobId { get; set; }
        public JobType JobType { get; set; }
        public int? Id { get; set; }
        public int RequeueCount { get; set; }
        public int? ExitCode { get; set; }
    }
}
