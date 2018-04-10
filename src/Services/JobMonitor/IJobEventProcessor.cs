namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    interface IJobEventProcessor
    {
        JobType RestrictedJobType { get; }
        string EventVerb { get; }
        Task ProcessAsync(Job job, JobEventMessage message, CancellationToken token);
    }
}
