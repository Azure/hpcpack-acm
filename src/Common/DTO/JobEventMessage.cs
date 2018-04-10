namespace Microsoft.HpcAcm.Common.Dto
{
    public class JobEventMessage
    {
        public int Id { get; set; }
        public JobType Type { get; set; }
        public string EventVerb { get; set; }
    }
}
