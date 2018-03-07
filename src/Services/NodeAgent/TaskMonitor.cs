namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TaskMonitor
    {
        public class TaskResultMonitor : IDisposable
        {
            private string key;
            private TaskMonitor monitor;
            public TaskResultMonitor(string key, TaskMonitor monitor)
            {
                this.key = key;
                this.monitor = monitor;
            }

            internal TaskCompletionSource<ComputeNodeTaskCompletionEventArg> commandResult = new TaskCompletionSource<ComputeNodeTaskCompletionEventArg>();
            public Task<ComputeNodeTaskCompletionEventArg> Execution { get => this.commandResult.Task; }
            private void Dispose(bool isDisposing)
            {
                if (isDisposing)
                {
                    this.monitor.taskResults.TryRemove(this.key, out _);
                }
            }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        private ConcurrentDictionary<string, TaskResultMonitor> taskResults = new ConcurrentDictionary<string, TaskResultMonitor>();

        public TaskResultMonitor StartMonitorTaskResult(string key)
        {
            return this.taskResults.GetOrAdd(key, new TaskResultMonitor(key, this));
        }

        public TaskResultMonitor GetTaskResultMonitor(string key)
        {
            return this.taskResults.TryGetValue(key, out TaskResultMonitor t) ? t : null;
        }

        public void CompleteTask(string key, ComputeNodeTaskCompletionEventArg commandResult)
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
