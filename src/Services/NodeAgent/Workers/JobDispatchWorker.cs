namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.HpcAcm.Services.Common;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using System.Threading;
    using T = System.Threading.Tasks;
    using Serilog;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using System.Diagnostics;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.DependencyInjection;

    internal class JobDispatchWorker : TaskItemWorker, IWorker
    {
        private readonly TaskItemSourceOptions options;

        public JobDispatchWorker(IOptions<TaskItemSourceOptions> options) : base(options.Value)
        {
            this.options = options.Value;
        }

        public override async T.Task InitializeAsync(CancellationToken token)
        {
            this.Source = new QueueTaskItemSource(
                await this.Utilities.GetOrCreateNodeDispatchQueueAsync(this.ServerOptions.HostName, token),
                this.options);
            await base.InitializeAsync(token);
        }

        public override async T.Task<bool> ProcessTaskItemAsync(TaskItem taskItem, CancellationToken token)
        {
            var message = taskItem.GetMessage<TaskEventMessage>();

            this.Logger.Information("Do work for TaskEvent {0}, {1}, {2}, message {3}", message.Id, message.JobType, message.EventVerb, taskItem.Id);

            try
            {
                // TODO: refactor the processor design.
                JobTaskProcessor processor = null;
                switch (message.EventVerb)
                {
                    case "cancel":
                        processor = this.Provider.GetService<CancelJobOrTaskProcessor>();
                        break;

                    case "start":
                        processor = this.Provider.GetService<StartJobAndTaskProcessor>();
                        break;

                    default:
                        break;
                }

                if (processor is ServerObject so)
                {
                    so.CopyFrom(this);
                }

                var result = await processor.ProcessAsync(message, taskItem.GetInsertionTime(), token);
                this.Logger.Information("Finished process {0} {1} {2}, result {3}", message.EventVerb, message.JobId, message.Id, result);
                return result;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception ex)
            {
                this.Logger.Error("Exception occurred when process {0}, {1}, {2}, {3}", message.EventVerb, message.JobId, message.Id, ex);

                await this.Utilities.FailJobWithEventAsync(
                    message.JobType,
                    message.JobId,
                    $"Exception occurred when process job {message.JobId} {message.JobType} {message.EventVerb}. {ex}",
                    token);
            }

            return true;
        }
    }
}
