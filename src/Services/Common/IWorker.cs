namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.HpcAcm.Common.Utilities;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IWorker
    {
        Task InitializeAsync(CancellationToken token);
        Task DoWorkAsync(CancellationToken token);
    }
}
