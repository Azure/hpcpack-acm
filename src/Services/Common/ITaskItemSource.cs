namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITaskItemSource
    {
        Task<TaskItem> FetchTaskItemAsync(CancellationToken token);
    }
}
