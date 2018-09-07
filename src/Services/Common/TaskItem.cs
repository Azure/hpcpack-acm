namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class TaskItem : IDisposable
    {
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }

        public virtual CancellationToken Token { get; } = default(CancellationToken);
        public virtual string Id { get; } = Guid.NewGuid().ToString();
        public virtual T GetMessage<T>() where T : class => default(T);
        public virtual DateTimeOffset? GetInsertionTime() => default(DateTimeOffset?);
        public virtual Task FinishAsync(CancellationToken token) => Task.CompletedTask;
        public virtual Task ReturnAsync(CancellationToken token) => Task.CompletedTask;
    }
}
