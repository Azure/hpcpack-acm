namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Runtime.Serialization;

    [DataContract()]
    public class MetricHistory
    {
        public class Record : Comparer<Record>
        {
            public DateTimeOffset Time { get; set; }
            public List<MetricItem> Data { get; set; }

            public override int Compare(Record x, Record y)
            {
                return x.Time.CompareTo(y.Time);
            }
        }

        public MetricHistory() { }
        public MetricHistory(TimeSpan range) { this.Range = range; this.Data = new List<Record>(); }

        [DataMember()]
        public double Span
        {
            get { return this.Range.TotalSeconds; }
            set { this.Range = TimeSpan.FromSeconds(value); }
        }

        [DataMember()]
        public List<Record> Items
        {
            get
            {
                if (this.Data != null)
                {
                    if (!this.sorted)
                    {
                        this.Data.Sort();
                        this.sorted = true;
                    }
                }
                return this.Data;
            }
            set { this.Data = value; }
        }

        private bool sorted = false;

        public TimeSpan Range { get; set; }
        public List<Record> Data { get; set; }

        public void Put(DateTimeOffset time, List<MetricItem> metrics)
        {
            var oldest = time - this.Range;
            this.Data = this.Data.Where(item => item.Time >= oldest).ToList();
            this.Data.Add(new Record() { Time = time, Data = metrics });
            this.sorted = false;
        }
    }
}
