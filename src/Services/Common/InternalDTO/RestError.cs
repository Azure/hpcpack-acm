namespace Microsoft.Hpc.Activation
{
    using System;

    [Serializable]
    public class RestError
    {
        public RestErrorCode Code { get; set; }

        public string Message { get; set; }
    }

    public enum RestErrorCode
    {
        MissingOrIncorrectVersionHeader,
    }
}
