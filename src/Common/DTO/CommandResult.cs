namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class CommandResult
    {
        public string ResultKey { get; set; }
        public DiagnosticsTest Test { get; set; }
        public string CommandLine { get; set; }
        public ComputeClusterTaskInformation TaskInfo { get; set; }
        public string NodeName { get; set; }
    }
}
