﻿namespace Microsoft.HpcAcm.Services.JobMonitor
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    interface IJobTypeHandler
    {
        T.Task<DispatchResult> DispatchAsync(Job job, CancellationToken token);
        T.Task<string> AggregateTasksAsync(Job job, List<Task> tasks, List<ComputeClusterTaskInformation> taskResults, CancellationToken token);
    }
}
