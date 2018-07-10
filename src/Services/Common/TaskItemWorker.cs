namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class TaskItemWorker : ServerObject, IWorker
    {
        protected TaskItemWorker(TaskItemSourceOptions taskItemSourceOptions)
        {
            this.TaskItemSourceOptions = taskItemSourceOptions;
        }

        protected ITaskItemSource Source { get; set; }
        private TaskItemSourceOptions TaskItemSourceOptions { get; }

        public virtual Task InitializeAsync(CancellationToken token)
        {
            if (this.Source is ServerObject so)
            {
                so.CopyFrom(this);
            }

            return Task.CompletedTask;
        }

        public async Task DoWorkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (var taskItem = await this.Source.FetchTaskItemAsync(token))
                    {
                        var success = await this.ProcessTaskItemAsync(taskItem, token);
                        await (success ? taskItem.FinishAsync(token) : taskItem.ReturnAsync(token));
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, $"Exception happened in {nameof(DoWorkAsync)}");
                    await Task.Delay(TimeSpan.FromSeconds(this.TaskItemSourceOptions.FailureRetryIntervalSeconds), token);
                }
            }
        }

        public abstract Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token);
    }
}
