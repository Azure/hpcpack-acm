namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    abstract class JobActionHandlerBase : ServerObject, IJobActionHandler
    {
        public IJobTypeHandler JobTypeHandler { get; set; }
        public abstract T.Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token);
    }
}
