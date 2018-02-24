namespace Microsoft.HpcAcm.Common.Dto
{
    public enum JobState
    {
        Queued,
        Running,
        Finished,
        Failed,
        Canceled,
    }

    public class Job
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public JobState State { get; private set; }
        public int Progress { get; private set; }

        public string[] TargetNodes { get; private set; }
    }
}