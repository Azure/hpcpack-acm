namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DashboardItem
    {
        public string LowerKey { get; set; }
        public string HigherKey { get; set; }
        public Dictionary<string, int> Statistics { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TotalStatistics { get; set; } = new Dictionary<string, int>();
        public DateTimeOffset NextUpdateTime { get; set; }
        public void MergeWith(DashboardItem that)
        {
            this.TotalStatistics = new Dictionary<string, int>(this.Statistics);
            if (that == null) return;
            foreach(var p in that.TotalStatistics)
            {
                if (this.TotalStatistics.TryGetValue(p.Key, out int v))
                {
                    this.TotalStatistics[p.Key] = v + p.Value;
                }
                else
                {
                    this.TotalStatistics[p.Key] = p.Value;
                }
            }
        }
    }
}
