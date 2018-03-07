namespace Microsoft.HpcAcm.Services.Common
{
    using Microsoft.HpcAcm.Common.Dto;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class InternalJob : Job
    {
        public static InternalJob CreateFrom(Job job)
        {
            var j = new InternalJob()
            {
                Id = job.Id,
                Name = job.Name,
                State = job.State,
                Progress = job.Progress,
                IsTaskExclusive = job.IsTaskExclusive,
                TargetNodes = (string[])job.TargetNodes.Clone(),
            };

            if (!string.IsNullOrEmpty(job.CommandLine))
            {
                j.CommandLines = new string[] { job.CommandLine };
            }
            else
            {
                j.CommandLines = job.DiagnosticTests.Select(t => t.CommandLine).ToArray();
            }

            return j;
        }

        public string[] CommandLines { get; set; }
    }
}
