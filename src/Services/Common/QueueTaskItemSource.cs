namespace Microsoft.HpcAcm.Services.Common
{
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
        private readonly TaskItemSourceOptions options;

        public QueueTaskItemSource(CloudQueue queue, TaskItemSourceOptions options)
        {
            this.queue = queue;
            this.options = options;
        }

        public async Task<TaskItem> FetchTaskItemAsync(CancellationToken token)
        {
            do
            {
                this.Logger.Debug("Fetching task item from queue {0}", this.queue.Name);

                var message = await this.queue.GetMessageAsync(TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds), null, null, token);
                if (message == null)
                {
                    this.Logger.Debug("No tasks fetched. Sleep for {0} seconds", this.options.RetryIntervalSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(this.options.RetryIntervalSeconds), token);
                }
                else
                {
                    return new QueueTaskItem(
                        message,
                        this.queue,
                        TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                        TimeSpan.FromSeconds(this.options.ReturnInvisibleSeconds),
                        this.Logger,
                        token);
                }
            }
            while (true);
        }
    }
}
