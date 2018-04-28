namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueTaskItemSource : ServerObject, ITaskItemSource
    {
        private readonly CloudQueue queue;
        private readonly TimeSpan visibleTimeout;
        private readonly TimeSpan retryInterval;

        public QueueTaskItemSource(CloudQueue queue, TimeSpan visibleTimeout, TimeSpan retryInterval)
        {
            this.queue = queue;
            this.visibleTimeout = visibleTimeout;
            this.retryInterval = retryInterval;
        }

        public async Task<TaskItem> FetchTaskItemAsync(CancellationToken token)
        {
            do
            {
                this.Logger.LogDebug("Fetching task item from queue {0}", this.queue.Name);

                var message = await this.queue.GetMessageAsync(visibleTimeout, null, null, token);
                if (message == null)
                {
                    this.Logger.LogDebug("No tasks fetched. Sleep for {0} seconds", this.retryInterval.TotalSeconds);
                    await Task.Delay(this.retryInterval, token);
                }
                else
                {
                    return new QueueTaskItem(message, this.queue, this.visibleTimeout, this.Logger, token);
                }
            }
            while (true);
        }
    }
}
