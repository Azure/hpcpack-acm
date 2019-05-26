namespace Microsoft.HpcAcm.Common.Dto
{
    public class ManagementResponse : ManagementResponseMessage
    {
        public string Result { get; set; }

        public string Error { get; set; }
    }
}
