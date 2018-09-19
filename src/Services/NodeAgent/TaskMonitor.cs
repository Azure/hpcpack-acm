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
        public class TaskEntry : IDisposable
        {
            private readonly TaskMonitor monitor;
            private readonly string key;
            private readonly int jobId;

            public TaskResultMonitor Result { get; }
            public OutputSorter Sorter { get; }

            public TaskEntry(int jobId, string key, TaskMonitor monitor, Func<string, bool, CancellationToken, T.Task> outputProcessor)
            {
                this.jobId = jobId;
                this.key = key;
                this.monitor = monitor;
                this.Result = new TaskResultMonitor();
                this.Sorter = new OutputSorter(outputProcessor);
            }

            protected virtual void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    this.monitor.taskEntries.TryGetValue(jobId, out var j);
                    j?.TryRemove(this.key, out _);
                    this.Sorter?.Dispose();
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
        public class TaskResultMonitor
        {
            internal T.TaskCompletionSource<ComputeNodeTaskCompletionEventArgs> CommandResult = new T.TaskCompletionSource<ComputeNodeTaskCompletionEventArgs>();
            public T.Task<ComputeNodeTaskCompletionEventArgs> Execution { get => this.CommandResult.Task; }
        }

        public class OutputSorter : IDisposable
        {
            public OutputSorter(Func<string, bool, CancellationToken, T.Task> processor)
            {
                this.processor = processor;
            }

            protected virtual void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    this.sem?.Dispose();
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
                            if (!this.cache.TryRemove(leftKey, out ClusrunOutput tmp))
                            {
                                break;
                            }
                            else
                            {
                                i = tmp;
                            }
                        }

                        await this.processor(builder.ToString(), i.Eof, token);
                        if (i != null && i.Eof)
                        {
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

        private readonly ConcurrentDictionary<int, ConcurrentDictionary<string, TaskEntry>> taskEntries = new ConcurrentDictionary<int, ConcurrentDictionary<string, TaskEntry>>();

        public TaskEntry StartMonitorTask(int jobId, string key, Func<string, bool, CancellationToken, T.Task> outputProcessor)
        {
            var jobDict = this.taskEntries.GetOrAdd(jobId, id => new ConcurrentDictionary<string, TaskEntry>());

            return jobDict.GetOrAdd(key, k => new TaskEntry(jobId, key, this, outputProcessor));
        }

        public TaskEntry GetTaskEntry(int jobId, string key) => this.taskEntries.TryGetValue(jobId, out var e) && e.TryGetValue(key, out var r) ? r : null;

        public TaskResultMonitor GetTaskResultMonitor(int jobId, string key) => this.GetTaskEntry(jobId, key)?.Result;
        public OutputSorter GetTaskOutputSorter(int jobId, string key) => this.GetTaskEntry(jobId, key)?.Sorter;

        public async T.Task PutOutput(int jobId, string key, ClusrunOutput output, CancellationToken token)
        {
            var sorter = this.GetTaskOutputSorter(jobId, key);
            if (sorter != null) await sorter.PutOutput(output, token);
        }

        public void CompleteTask(int jobId, string key, ComputeNodeTaskCompletionEventArgs commandResult)
        {
            using (var m = this.GetTaskEntry(jobId, key))
            {
                m?.Result?.CommandResult.SetResult(commandResult);
            }
        }

        public void FailTask(int jobId, string key, Exception ex)
        {
            using (var m = this.GetTaskEntry(jobId, key))
            {
                m?.Result?.CommandResult?.SetException(ex);
            }
        }

        public void CancelJob(int jobId)
        {
            if (this.taskEntries.TryRemove(jobId, out var entries))
            {
                foreach (var m in entries)
                {
                    m.Value.Dispose();
                }
            }
        }

        public void CancelTask(int jobId, string key)
        {
            using (var m = this.GetTaskEntry(jobId, key))
            {
                m?.Result?.CommandResult?.SetCanceled();
            }
        }
    }
}
