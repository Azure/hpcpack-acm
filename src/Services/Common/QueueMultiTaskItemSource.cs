namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueMultiTaskItemSource : ServerObject, ITaskItemSource
    {
        private readonly CloudQueue queue;
        private readonly TaskItemSourceOptions options;

        public QueueMultiTaskItemSource(CloudQueue queue, TaskItemSourceOptions options)
        {
            this.queue = queue;
            this.options = options;
        }

        public async Task<TaskItem> FetchTaskItemAsync(CancellationToken token)
        {
            do
            {
                this.Logger.Debug("Fetching task items from queue {0}", this.queue.Name);

                var messages = await this.queue.GetMessagesAsync(32, TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds), null, null, token);
                if (messages == null || messages.Count() == 0)
                {
                    this.Logger.Debug("No tasks fetched. Sleep for {0} seconds", this.options.RetryIntervalSeconds);
                    await Task.Delay(TimeSpan.FromSeconds(this.options.RetryIntervalSeconds), token);
                }
                else
                {
                    return new MultiTaskItem(messages.Select(msg => new QueueTaskItem(
                        msg,
                        this.queue,
                        TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                        TimeSpan.FromSeconds(this.options.ReturnInvisibleSeconds),
                        this.Logger,
                        token)).ToArray());
                }
            }
            while (true);
        }
    }
}
