namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.WindowsAzure.Storage;
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
            try
            {
                this.Logger.Debug("Fetching task item from queue {0}", this.queue.Name);

                var message = await this.queue.GetMessageAsync(TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds), null, null, token);
                if (message == null)
                {
                    return null;
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
            catch (StorageException ex) when (ex.IsCancellation())
            {
                throw ex.InnerException;
            }
        }
    }
}
