namespace Microsoft.HpcAcm.Services.Dashboard
{
    using Microsoft.HpcAcm.Services.Common;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DashboardOptions
    {
        public int AggregationPeriodSeconds { get; set; } = 30;
        public int BatchCount { get; set; } = 100;
        public int ActiveUpdateIntervalSeconds { get; set; } = 30;
        public int InactiveUpdateIntervalSeconds { get; set; } = 3600;
    }
}
