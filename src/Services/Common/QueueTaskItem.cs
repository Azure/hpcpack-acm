namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueTaskItem : TaskItem
    {
        // TODO: make visible, keep invisible.
        private CloudQueueMessage QueueMessage { get; }
        private CloudQueue Queue { get; }

        public QueueTaskItem(CloudQueueMessage message, CloudQueue queue)
        {
            this.QueueMessage = message;
            this.Queue = queue;
        }

        public override T GetMessage<T>() => JsonConvert.DeserializeObject<T>(this.QueueMessage.AsString);

        public override async Task FinishAsync(CancellationToken token)
        {
            await this.Queue.DeleteMessageAsync(this.QueueMessage.Id, this.QueueMessage.PopReceipt, null, null, token);
        }
    }
}
