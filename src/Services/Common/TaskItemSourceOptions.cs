namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskItemSourceOptions
    {
        public int VisibleTimeoutSeconds { get; set; } = 60;
        public int ReturnInvisibleSeconds { get; set; } = 5;
        public int RetryIntervalSeconds { get; set; } = 2;
        public int FailureRetryIntervalSeconds { get; set; } = 5;
    }
}
