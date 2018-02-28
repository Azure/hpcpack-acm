namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;

    public class DiagnosticsJob : Job
    {
        public IList<DiagnosticsTest> DiagnosticTests { get; private set; }
    }
}