namespace Microsoft.HpcAcm.Common.Dto
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class TaskStartInfo
    {
        public TaskStartInfo() { }
        public JobType JobType { get; set; }
        public int JobId { get; set; }
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string PrivateKey { get; set; }
        public string PublicKey { get; set; }
        public string NodeName { get; set; }
        public ProcessStartInfo StartInfo { get; set; }
    }
}
