namespace Microsoft.HpcAcm.Common.Dto
{
    using System.Collections.Generic;

    public class DiagnosticsTest
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public Dictionary<string, object>[] Arguments { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object>[] Parameters { get; set; }

    }
}
