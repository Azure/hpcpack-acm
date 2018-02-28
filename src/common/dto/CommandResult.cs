namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class CommandResult
    {
        public DiagnosticsTest Test { get; set; }
        public string CommandLine { get; private set; }
        public string Output { get; private set; }
        public int ExitCode { get; private set; }
        public string NodeName { get; private set; }
    }
}
