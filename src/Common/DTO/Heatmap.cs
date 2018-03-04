namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;

    public class Heatmap
    {
        public IDictionary<string, double> Values { get; private set; } 
        public string MetricName { get; private set; }
    }
}