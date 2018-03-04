namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("api/[controller]")]
    public class HeatmapController : Controller
    {
        // POST api/heatmap/cpu
        // Post body contains nodes filtering information.
        [HttpPost("{metricName}")]
        public async Task<Heatmap> GetAsync(string metricName)
        {
            await Task.CompletedTask;
            return new Heatmap();
        }
    }
}
