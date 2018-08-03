namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskOutputPage
    {
        public const string EofMark = "Eof";
        public bool Eof { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }
        public string Content { get; set; }
    }
}
