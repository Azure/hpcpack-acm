namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Runtime.Serialization;

    public class MetricHistory
    {
        public class Record
        {
            public DateTimeOffset Time { get; set; }
            public List<MetricItem> MetricItems { get; set; }
        }

        public MetricHistory() { }
        public MetricHistory(TimeSpan range) { this.RangeSeconds = range.TotalSeconds; }

        public double RangeSeconds { get; set; }
        public List<Record> Data { get; set; } = new List<Record>();

        public void Put(DateTimeOffset time, List<MetricItem> metrics)
        {
            var oldest = time - TimeSpan.FromSeconds(this.RangeSeconds);
            this.Data = this.Data.Where(item => item.Time >= oldest).ToList();
            this.Data.Add(new Record() { Time = time, MetricItems = metrics });
        }
    }
}
