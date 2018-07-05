namespace Microsoft.HpcAcm.Services.Dashboard
{
    using Microsoft.Extensions.Options;
    using Microsoft.HpcAcm.Common.Dto;
    using Microsoft.HpcAcm.Common.Utilities;
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using T = System.Threading.Tasks;

    internal class ClusrunJobDashboardWorker : JobDashboardWorker
    {
        public ClusrunJobDashboardWorker(IOptions<DashboardOptions> options) : base(options)
        {
        }

        public override JobType Type { get => JobType.ClusRun; }
    }
}
