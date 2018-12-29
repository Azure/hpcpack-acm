namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class DispatchResult
    {
        public Job ModifiedJob { get; set; }
        public List<InternalTask> Tasks { get; set; }
    }
}
