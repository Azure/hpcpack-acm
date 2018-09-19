namespace Microsoft.HpcAcm.Services.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskItemSourceOptions
    {
        public int ThrottleMessageCount { get; set; } = 500;
        public int VisibleTimeoutSeconds { get; set; } = 60;
        public int ReturnInvisibleSeconds { get; set; } = 5;
        public double RetryIntervalSeconds { get; set; } = 0.1;
        public int FailureRetryIntervalSeconds { get; set; } = 5;
    }
}
