namespace Microsoft.HpcAcm.Frontend.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.HpcAcm.Common.Dto;

    [Route("api/[controller]")]
    public class HeatmapController : Controller
    {
        // GET api/heatmap/values/cpu?
        [HttpGet("{category}")]
        public async Task<Heatmap> GetValuesAsync(string category, [FromQuery] string lastNodeName, [FromQuery] int? count, CancellationToken token)
        {
            await Task.CompletedTask;
            return new Heatmap();
        }

        // GET api/heatmap/categories
        [HttpGet]
        public async Task<string[]> GetCategoriesAsync(CancellationToken token)
        {
            return await Task.FromResult(new string[] { "cpu" });
        }
    }
}
