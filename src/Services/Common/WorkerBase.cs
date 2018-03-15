namespace Microsoft.HpcAcm.Services.Common
{
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class WorkerBase
    {
        public abstract Task<bool> DoWorkAsync(TaskItem taskItem, CancellationToken token);
    }
}
