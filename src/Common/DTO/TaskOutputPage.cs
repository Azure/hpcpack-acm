namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskOutputPage
    {
        public long Offset { get; set; }
        public long Size { get; set; }
        public byte[] Content { get; set; }
    }
}
