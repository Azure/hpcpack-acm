namespace Microsoft.HpcAcm.Services.TaskDispatcher
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class TaskDispatcherWorkerGroup : WorkerGroup<TaskDispatcherWorker>
    {
        public TaskDispatcherWorkerGroup(IOptions<TaskDispatcherWorkerGroupOptions> options) : base(options)
        {

        }
    }
}
