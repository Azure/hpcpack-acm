namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueMultiTaskItemSource : ServerObject, ITaskItemSource, IDisposable
    {
        private readonly CloudQueue queue;
        private readonly TaskItemSourceOptions options;
        private int firstFetch = 1;

        public QueueMultiTaskItemSource(CloudQueue queue, TaskItemSourceOptions options)
        {
            this.queue = queue;
            this.options = options;
        }

        private ConcurrentQueue<QueueTaskItem> cacheQueue = new ConcurrentQueue<QueueTaskItem>();
        private Task FetchingTasks;
        private CancellationTokenSource cts = new CancellationTokenSource();

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.cts?.Cancel();
                this.FetchingTasks?.GetAwaiter().GetResult();
                this.cts?.Dispose();

                Parallel.ForEach(this.cacheQueue, item => item.Dispose());
            }

            this.cts = null;
            this.FetchingTasks = null;
            this.cacheQueue = null;
        }

        public Task<TaskItem> FetchTaskItemAsync(CancellationToken token)
        {
            var t = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token).Token;
            if (Interlocked.Exchange(ref this.firstFetch, 0) == 1)
            {
                // start the fetch process when first fetch, this is to use the token, and avoid unnecessary dequeue of the messages.
                this.FetchingTasks = Task.WhenAll(Enumerable.Range(0, 16).Select(async d =>
                {
                    while (!t.IsCancellationRequested)
                    {
                        try
                        {
                            this.Logger.Debug("Fetching task items from queue {0}", this.queue.Name);
                            var messages = this.cacheQueue.Count >= this.options.ThrottleMessageCount ? null : await this.queue.GetMessagesAsync(32, TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds), null, null, t);

                            if (messages == null || messages.Count() == 0)
                            {
                                this.Logger.Debug("No tasks fetched. Sleep for {0} seconds", this.options.RetryIntervalSeconds);
                                await Task.Delay(TimeSpan.FromSeconds(this.options.RetryIntervalSeconds), t);
                            }
                            else
                            {
                                foreach (var msg in messages)
                                {
                                    this.cacheQueue.Enqueue(new QueueTaskItem(
                                        msg,
                                        this.queue,
                                        TimeSpan.FromSeconds(this.options.VisibleTimeoutSeconds),
                                        TimeSpan.FromSeconds(this.options.ReturnInvisibleSeconds),
                                        this.Logger,
                                        t));
                                }
                            }
                        }
                        catch (StorageException ex)
                        {
                            if (ex.InnerException is OperationCanceledException)
                            {
                                continue;
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            this.Logger.Error(ex, "Error happened in fetching message loop.");
                        }
                    }

                    this.Logger.Information("Exiting fetching message loop");
                }));
            }

            List<QueueTaskItem> items = new List<QueueTaskItem>();
            while (this.cacheQueue.TryDequeue(out QueueTaskItem item))
            {
                items.Add(item);
            }

            if (items.Count == 0)
            {
                return Task.FromResult<TaskItem>(null);
            }
            else
            {
                return Task.FromResult<TaskItem>(new MultiTaskItem(items.ToArray()));
            }
        }
    }
}
