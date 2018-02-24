namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;

    public class NodeDetails
    {
        public Node NodeInfo { get; private set; }
        public IList<Event> Events { get; private set; }
        public IList<DiagnosticsJob> Jobs { get; private set; }
        public IDictionary<string, IDictionary<DateTime, double>> MetricHistories { get; private set; }
    }
}