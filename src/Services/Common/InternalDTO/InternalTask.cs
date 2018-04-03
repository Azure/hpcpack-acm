using Microsoft.HpcAcm.Common.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.HpcAcm.Services.Common
{
    public class InternalTask
    {
        public static InternalTask CreateFrom(Job job)
        {
            return new InternalTask()
            {
                JobId = job.Id,
                RequeueCount = job.RequeueCount,
                JobType = job.Type,
                CommandLine = job.CommandLine,
            };
        }

        public int JobId { get; set; }
        public int Id { get; set; }
        public int RequeueCount { get; set; }
        public JobType JobType { get; set; }
        public List<int> ParentsIds { get; set; }
        public List<int> ChildrenIds { get; set; }
        public HashSet<int> RemainingParentIds { get; set; }
        public string CommandLine { get; set; }
        public string Node { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
    }
}
