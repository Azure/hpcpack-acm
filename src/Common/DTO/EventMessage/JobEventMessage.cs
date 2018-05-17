namespace Microsoft.HpcAcm.Common.Dto
{
    public class JobEventMessage : EventMessage
    {
        public int Id { get; set; }
        public JobType Type { get; set; }
    }
}
