namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;

    public class DiagnosticsJob : Job
    {
        public IList<string> DiagnosticTests { get; private set; }
    }
}