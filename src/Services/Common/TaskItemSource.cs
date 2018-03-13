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

    public class TaskItemSource
    {
        private readonly CloudQueue queue;
        private readonly TimeSpan visibleTimeout;
        private readonly ILogger logger;

        public TaskItemSource(CloudQueue queue, TimeSpan visibleTimeout, ILoggerFactory loggerFactory)
        {
            this.queue = queue;
            this.logger = loggerFactory.CreateLogger<TaskItemSource>();
            this.visibleTimeout = visibleTimeout;
        }

        public async Task<TaskItem> FetchTaskItemAsync(CancellationToken token)
        {
            this.logger.LogInformation("Fetching task item from queue {0}", this.queue.Name);
            var message = await this.queue.GetMessageAsync(visibleTimeout, null, null, token);
            return message == null ? null : new TaskItem(message, this);
        }

        public async Task FinishTaskItemAsync(TaskItem item, CancellationToken token)
        {
            await this.queue.DeleteMessageAsync(item.QueueMessage.Id, item.QueueMessage.PopReceipt, null, null, token);
        }
    }
}
