namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;

    public class NodeDetails
    {
        public Node NodeInfo { get; set; }
        public IList<Event> Events { get; set; }
        public IList<ComputeClusterJobInformation> Jobs { get; set; }
        public MetricHistory History { get; set; }
    }
}