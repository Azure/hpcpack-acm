namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using T = System.Threading.Tasks;

    public class TaskMonitor
    {
        public class TaskResultMonitor : IDisposable
        {
            private readonly string key;
            private readonly TaskMonitor monitor;

            public TaskResultMonitor(string key, TaskMonitor monitor)
            {
                this.key = key;
                this.monitor = monitor;
            }

            internal T.TaskCompletionSource<ComputeNodeTaskCompletionEventArgs> commandResult = new T.TaskCompletionSource<ComputeNodeTaskCompletionEventArgs>();
            public T.Task<ComputeNodeTaskCompletionEventArgs> Execution { get => this.commandResult.Task; }

            protected virtual void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    this.monitor.taskResults.TryRemove(this.key, out _);
                    this.monitor.taskOutputs.TryRemove(this.key, out _);
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        public class OutputSorter : IDisposable
        {
            private readonly string key;
            private readonly TaskMonitor monitor;

            public OutputSorter(string key, TaskMonitor monitor, Func<string, bool, CancellationToken, T.Task> processor)
            {
                this.processor = processor;
                this.key = key;
                this.monitor = monitor;
            }

            protected virtual void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    this.monitor.taskOutputs.TryRemove(this.key, out _);
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private readonly ConcurrentDictionary<int, ClusrunOutput> cache = new ConcurrentDictionary<int, ClusrunOutput>();
            private int leftKey;
            private int rightKey;

            public async T.Task PutOutput(ClusrunOutput output, CancellationToken token)
            {
                await this.sem.WaitAsync(token);

                try
                {
                    if (output.Order == leftKey)
                    {
                        StringBuilder builder = new StringBuilder();

                        var i = output;

                        while (!i.Eof)
                        {
                            builder.Append(i.Content);

                            leftKey++;
                            if (!this.cache.TryRemove(leftKey, out i))
                            {
                                break;
                            }
                        }

                        await this.processor(builder.ToString(), i.Eof, token);
                        if (i != null && i.Eof)
                        {
                            this.Dispose();
                            return;
                        }
                    }
                    else
                    {
                        this.cache.TryAdd(output.Order, output);
                        this.rightKey = Math.Max(this.rightKey, output.Order);
                    }
                }
                finally
                {
                    this.sem.Release();
                }
            }

            private readonly SemaphoreSlim sem = new SemaphoreSlim(1);
            private readonly Func<string, bool, CancellationToken, T.Task> processor;
        }

        private readonly ConcurrentDictionary<string, TaskResultMonitor> taskResults = new ConcurrentDictionary<string, TaskResultMonitor>();
        private readonly ConcurrentDictionary<string, OutputSorter> taskOutputs = new ConcurrentDictionary<string, OutputSorter>();

        public TaskResultMonitor StartMonitorTask(string key, Func<string, bool, CancellationToken, T.Task> outputProcessor)
        {
            this.taskOutputs.GetOrAdd(key, new OutputSorter(key, this, outputProcessor));
            return this.taskResults.GetOrAdd(key, new TaskResultMonitor(key, this));
        }

        public TaskResultMonitor GetTaskResultMonitor(string key)
        {
            return this.taskResults.TryGetValue(key, out TaskResultMonitor t) ? t : null;
        }

        public T.Task PutOutput(string key, ClusrunOutput output, CancellationToken token)
        {
            return this.taskOutputs.TryGetValue(key, out OutputSorter sorter) ? sorter.PutOutput(output, token) : T.Task.CompletedTask;
        }

        public void CompleteTask(string key, ComputeNodeTaskCompletionEventArgs commandResult)
        {
            using (var m = this.GetTaskResultMonitor(key))
            {
                m?.commandResult?.SetResult(commandResult);
            }
        }

        public void FailTask(string key, Exception ex)
        {
            using (var m = this.GetTaskResultMonitor(key))
            {
                m?.commandResult?.SetException(ex);
            }
        }

        public void CancelTask(string key)
        {
            using (var m = this.GetTaskResultMonitor(key))
            {
                m?.commandResult?.SetCanceled();
            }
        }
    }
}
