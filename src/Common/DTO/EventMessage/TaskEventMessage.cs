namespace Microsoft.HpcAcm.Common.Dto
{
    public class TaskEventMessage : EventMessage
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int RequeueCount { get; set; }
        public JobType JobType { get; set; }
    }
}
