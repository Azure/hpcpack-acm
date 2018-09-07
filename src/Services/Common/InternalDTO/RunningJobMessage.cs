namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class RunningJobMessage
    {
        public int JobId { get; set; }
        public JobType JobType { get; set; }
        public int RequeueCount { get; set; }
    }
}
