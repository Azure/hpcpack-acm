namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MetricHistory
    {
        public MetricHistory() { }
        public MetricHistory(TimeSpan range) { this.Range = range; this.Data = new SortedDictionary<DateTimeOffset, List<MetricItem>>(); }
        public TimeSpan Range { get; set; }
        public SortedDictionary<DateTimeOffset, List<MetricItem>> Data { get; set; }

        public void Put(DateTimeOffset time, List<MetricItem> metrics)
        {
            var oldest = time - this.Range;
            var keys = this.Data.Keys.Where(k => k < oldest).ToList();

            keys.ForEach(k => this.Data.Remove(k));
            this.Data[time] = metrics;
        }
    }
}
