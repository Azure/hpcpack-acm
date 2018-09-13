namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;

    [Authorize]
    [Route("v1/sync")]
    public class SyncScriptV1Controller : Controller
    {
        private readonly DataProvider provider;

        public SyncScriptV1Controller(DataProvider provider) { this.provider = provider; }

        [HttpPost()]
        public Task<IActionResult> Sync(CancellationToken token) => this.provider.RequestScriptSyncAsync(token);
    }
}