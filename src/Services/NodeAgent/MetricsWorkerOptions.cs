using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.HpcAcm.Services.NodeAgent
{
    public class MetricsWorkerOptions
    {
        public int MetricsIntervalSeconds { get; set; } = 2;
    }
}
