namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ClusrunOutput
    {
        public int Order { get; set; }

        public string NodeName { get; set; }
        public string Content { get; set; } = $"{Environment.NewLine}<Missed Message>{Environment.NewLine}";
        public bool Eof { get; set; }
    }
}
