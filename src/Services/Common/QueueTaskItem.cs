﻿namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Newtonsoft.Json;
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

        private readonly Task ensureInvisibilityTask;

        private readonly ILogger logger;

        public QueueTaskItem(CloudQueueMessage message, CloudQueue queue, TimeSpan invisibleTimeout, ILogger logger, CancellationToken token)
        {
            this.logger = logger;
            this.QueueMessage = message;
            this.Queue = queue;
            this.invisibleTimeout = invisibleTimeout;

            this.ensureInvisibilityTask = this.EnsureInvisible(token);
        }

        private async Task MakeVisible(CancellationToken token)
        {
            await this.Queue.UpdateMessageAsync(this.QueueMessage, TimeSpan.Zero, MessageUpdateFields.Visibility, null, null, token);
        }

        private async Task EnsureInvisible(CancellationToken token)
        {
            try
            {
                var t = CancellationTokenSource.CreateLinkedTokenSource(token, this.cts.Token).Token;
                var loopInterval = this.invisibleTimeout - TimeSpan.FromSeconds(1.0);

                while (!t.IsCancellationRequested)
                {
                    await Task.Delay(loopInterval, t);

                    await this.Queue.UpdateMessageAsync(this.QueueMessage, this.invisibleTimeout, MessageUpdateFields.Visibility, null, null, t);
                }
            }
            catch (OperationCanceledException)
            {
                this.logger.LogInformation("Ensure Invisible stopped for message {0}", this.QueueMessage.Id);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                if (!this.cts.IsCancellationRequested)
                {
                    this.StopEnsureInvisible().Wait();
                }

                this.cts.Dispose();
                this.cts = null;
            }

            base.Dispose(isDisposing);
        }

        public override T GetMessage<T>() => JsonConvert.DeserializeObject<T>(this.QueueMessage.AsString);

        public async Task StopEnsureInvisible()
        {
            this.cts.Cancel();
            await this.ensureInvisibilityTask;
        }

        public override async Task ReturnAsync(CancellationToken token)
        {
            await this.StopEnsureInvisible();
            await this.MakeVisible(token);
        }

        public override async Task FinishAsync(CancellationToken token)
        {
            await this.StopEnsureInvisible();
            await this.Queue.DeleteMessageAsync(this.QueueMessage.Id, this.QueueMessage.PopReceipt, null, null, token);
        }
    }
}