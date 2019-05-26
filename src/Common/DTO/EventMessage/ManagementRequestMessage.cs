namespace Microsoft.HpcAcm.Common.Dto
{
    public class ManagementRequestMessage
    {
        public string OperationId { get; set; }

        public ManagementOperation Operation { get; set; }
    }
}
