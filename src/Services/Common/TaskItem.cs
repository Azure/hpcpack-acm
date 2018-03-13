namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class TaskItem
    {
        internal CloudQueueMessage QueueMessage;
        private readonly TaskItemSource source;

        public TaskItem(CloudQueueMessage message, TaskItemSource source)
        {
            this.QueueMessage = message;
            this.source = source;
        }

        public T GetMessage<T>() => JsonConvert.DeserializeObject<T>(this.QueueMessage.AsString);

        public async Task FinishAsync(CancellationToken token)
        {
            await this.source.FinishTaskItemAsync(this, token);
        }
    }
}
