namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;

    public class Heatmap
    {
        public class Record
        {
            public string Node { get; set; }

            public Dictionary<string, double?> Data { get; set; }
        }
        /// <summary>
        /// [ "node1": [ { "_Total": 0.5 }, { "_1": 0.2 }, { "_2", 0.3 } ],
        ///     "node2": ...
        /// ]
        /// </summary>
        public List<Record> Values { get; set; }
        public string Category { get; set; }
    }
}