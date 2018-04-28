namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class InternalDiagnosticsTest : DiagnosticsTest
    {
        public class BlobName
        {
            public string ContainerName { get; set; }
            public string Name { get; set; }
        }

        public BlobName DispatchScript { get; set; }
        public BlobName AggregationScript { get; set; }
        public BlobName TaskResultFilterScript { get; set; }
    }
}
