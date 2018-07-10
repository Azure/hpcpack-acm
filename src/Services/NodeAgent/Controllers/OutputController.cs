namespace Microsoft.HpcAcm.Services.NodeAgent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Serilog;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;

    [Produces("application/json")]
    [Route("api/[controller]")]
    public class OutputController : Controller
    {
        private readonly ILogger logger;
        private readonly TaskMonitor monitor;
        private CloudUtilities Utilities { get; }

        public OutputController(ILogger logger, TaskMonitor monitor, CloudUtilities utilities)
        {
            this.logger = logger;
            this.monitor = monitor;
            this.Utilities = utilities;
        }

        [HttpPost("{taskkey}")]
        public async Task MessageAsync(string taskKey, [FromBody] ClusrunOutput output, CancellationToken token)
        {
            try
            {
                this.logger.Information("TaskMessage {0}, order {1}, eof {2}", taskKey, output.Order, output.Eof);
                await this.monitor.PutOutput(taskKey, output, token);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error occurred, TaskMessage {0}, order {1}, eof {2}", taskKey, output.Order, output.Eof);
            }
        }
    }
}