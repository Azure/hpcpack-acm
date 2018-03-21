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
        // GET api/heatmap/values/cpu?lastNodeName=abc&count=5
        [HttpGet("values/{category}")]
        public async Task<Heatmap> GetValuesAsync(string category, [FromQuery] string lastNodeName, [FromQuery] int? count, CancellationToken token)
        {
            await Task.CompletedTask;

            var list = new List<(string InstanceName, double? Value)>()
            {
                ( "_Total", 0.5 ),
                ( "_1", 0.3 ),
                ( "_2", 0.2 ),
            };

            return new Heatmap()
            {
                Category = category,
                Values = new Dictionary<string, IList<(string, double?)>>()
                {
                    { "node1", list },
                    { "node2", list }
                }
            };
            //      return new Heatmap() { Category = category, Values = new Dictionary<string, IList> }
        }

        // GET api/heatmap/categories
        [HttpGet("categories")]
        public async Task<string[]> GetCategoriesAsync(CancellationToken token)
        {
            return await Task.FromResult(new string[] { "cpu" });
        }
    }
}
