namespace Microsoft.HpcAcm.Services.JobDispatcher
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    interface IDispatcher
    {
        JobType RestrictedJobType { get; }
        Task DispatchAsync(Job job, CancellationToken token);
    }
}
