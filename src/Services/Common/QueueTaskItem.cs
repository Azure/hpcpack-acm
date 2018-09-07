namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class QueueTaskItem : TaskItem
    {
        private CloudQueueMessage QueueMessage { get; }
        private CloudQueue Queue { get; }

        private CancellationTokenSource cts = new CancellationTokenSource();

        private readonly TimeSpan invisibleTimeout;
        private readonly TimeSpan returnInvisible;

        private readonly Thread ensureInvisibilityThread;

        private readonly ILogger logger;

        public QueueTaskItem(CloudQueueMessage message, CloudQueue queue, TimeSpan invisibleTimeout, TimeSpan returnInvisible, ILogger logger, CancellationToken token)
        {
            this.logger = logger;
            this.QueueMessage = message;
            this.Queue = queue;
            this.invisibleTimeout = invisibleTimeout;
            this.returnInvisible = returnInvisible;

            this.Token = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token).Token;

            this.ensureInvisibilityThread = new Thread(() => this.EnsureInvisible(this.Token))
            {
                Priority = ThreadPriority.AboveNormal,
                Name = $"EnsureInvisible {message.Id}"
            };

            this.ensureInvisibilityThread.Start();
            this.logger.Information("Constructed task item {0}", this.QueueMessage.Id);
        }

        public override CancellationToken Token { get; }
        public override string Id { get => this.QueueMessage.Id; }

        private async Task MakeVisible(CancellationToken token)
        {
            await this.Queue.UpdateMessageAsync(this.QueueMessage, this.returnInvisible, MessageUpdateFields.Visibility, null, null, token);
        }

        private void EnsureInvisible(CancellationToken token)
        {
            try
            {
                var loopInterval = this.invisibleTimeout - TimeSpan.FromSeconds(this.invisibleTimeout.TotalSeconds / 2);

                while (!token.IsCancellationRequested)
                {
                    Task.Delay(loopInterval, token).GetAwaiter().GetResult();

                    this.logger.Information("Ensure Invisible for message {0}", this.QueueMessage.Id);

                    try
                    {
                        this.Queue.UpdateMessageAsync(this.QueueMessage, this.invisibleTimeout, MessageUpdateFields.Visibility, null, null, token).GetAwaiter().GetResult();
                    }
                    catch (StorageException ex)
                    {
                        if (ex.InnerException is OperationCanceledException)
                        {
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                this.logger.Information("Ensure Invisible canceled for message {0}", this.QueueMessage.Id);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error when ensure invisible of a task message {0}", this.QueueMessage.Id);
                this.cts.Cancel();
            }

            this.logger.Information("Exiting Ensure Invisible for message {0}", this.QueueMessage.Id);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (!this.cts.IsCancellationRequested)
                {
                    this.logger.Information("Dispose: Stop ensure invisible for message {0}", this.QueueMessage.Id);
                    this.StopEnsureInvisible();
                }

                this.cts?.Dispose();
            }

            this.cts = null;
            base.Dispose(isDisposing);
        }

        public override T GetMessage<T>() => JsonConvert.DeserializeObject<T>(this.QueueMessage.AsString);
        public override DateTimeOffset? GetInsertionTime() => this.QueueMessage.InsertionTime;

        public void StopEnsureInvisible()
        {
            this.cts.Cancel();
            this.ensureInvisibilityThread.Join();
        }

        public override async Task ReturnAsync(CancellationToken token)
        {
            this.StopEnsureInvisible();
            await this.MakeVisible(token);
            this.logger.Information("ReturnAsync: Make visible for message {0}", this.QueueMessage.Id);
        }

        public override async Task FinishAsync(CancellationToken token)
        {
            this.StopEnsureInvisible();
            this.logger.Information("FinishAsync: Deleting message {0}", this.QueueMessage.Id);

            try
            {
                await this.Queue.DeleteMessageAsync(this.QueueMessage.Id, this.QueueMessage.PopReceipt, null, null, token);
                this.logger.Information("FinishAsync: Deleted message {0}", this.QueueMessage.Id);
            }
            catch (StorageException ex)
            {
                if (ex.InnerException is OperationCanceledException)
                {
                    return;
                }

                if (string.Equals(ex.RequestInformation.ErrorCode, "MessageNotFound", StringComparison.OrdinalIgnoreCase))
                {
                    this.logger.Warning("Deleting message {0}, not found", this.QueueMessage.Id);
                    return;
                }

                this.logger.Error(ex, "Deleting message {0}", this.QueueMessage.Id);
                throw;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Deleting message {0}", this.QueueMessage.Id);
                throw;
            }
        }
    }
}
