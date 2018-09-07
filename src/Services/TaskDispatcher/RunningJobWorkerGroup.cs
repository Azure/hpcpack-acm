namespace Microsoft.HpcAcm.Services.TaskDispatcher
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class RunningJobWorkerGroup : WorkerGroup<RunningJobWorker>
    {
        public RunningJobWorkerGroup(IOptions<RunningJobWorkerGroupOptions> options) : base(options)
        {

        }
    }
}
