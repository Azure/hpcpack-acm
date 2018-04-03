namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class InternalDiagnosticsTest : DiagnosticsTest
    {
        public string DispatchScript { get; set; }
        public string AggregationScript { get; set; }
    }
}
