using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.HpcAcm.Services.Common
{
    public class TaskItemSource
    {
        private CloudQueue queue;
        private TimeSpan visibleTimeout;

        public TaskItemSource(CloudQueue queue, TimeSpan visibleTimeout)
        {
            this.queue = queue;
            this.visibleTimeout = visibleTimeout;
        }

        public async Task<TaskItem> FetchTaskItemAsync(CancellationToken token)
        {
            var message = await this.queue.GetMessageAsync(visibleTimeout, null, null, token);
            return message == null ? null : new TaskItem(message, this);
        }

        public async Task FinishTaskItemAsync(TaskItem item, CancellationToken token)
        {
            await this.queue.DeleteMessageAsync(item.QueueMessage.Id, item.QueueMessage.PopReceipt, null, null, token);
        }
    }
}
