namespace Microsoft.HpcAcm.Services.Common
{
    #region Using directives

    using System;
    using System.Diagnostics;
    using System.Collections;

    #endregion


    [Serializable()]
    public sealed class ProcessStartInfo
    {
        #region PrivateFields
        /// <summary>
        /// the command line to execute.
        /// </summary>
        public string commandLine { get; set; }

        /// <summary>
        /// Search paths for files, directories for temporary files, application-specific options, and other similar information.
        /// </summary>
        public IDictionary environmentVariables { get; set; }

        /// <summary>
        /// The initial directory for the process to be started.
        /// </summary>
        public string workingDirectory { get; set; }

        /// <summary>
        /// standard input to the application. can be null.
        /// </summary>
        public string stdin { get; set; }

        /// <summary>
        /// standard output of the application. can be null.
        /// </summary>
        public string stdout { get; set; }

        /// <summary>
        /// standard error output of the application. can be null.
        /// </summary>
        public string stderr { get; set; }

        /// <summary>
        /// CPU masks for this process
        /// </summary>
        public Int64[] affinity { get; set; }

        /// <summary>
        /// Requeue count of the application (task)
        /// </summary>
        public int taskRequeueCount { get; set; } = 0;

        #endregion // PrivateFields

        #region Constructors
        public ProcessStartInfo(
                string commandLine,
                string workingDirectory,
                string stdin,
                string stdout,
                string stderr,
                Hashtable environmentVariables
                )
        {
            this.commandLine = commandLine;
            this.workingDirectory = workingDirectory;

            // Per V3 bug 1243, if the stdin/out/err are empty, just make them null
            // so that later the node manager will know it is null, and won't be replaced to "c:\users\xxx\"

            this.stdin = (stdin == string.Empty ? null : stdin);
            this.stdout = (stdout == string.Empty ? null : stdout);
            this.stderr = (stderr == string.Empty ? null : stderr);

            this.environmentVariables = environmentVariables;
        }

        public ProcessStartInfo(
                string commandLine,
                string workingDirectory,
                string stdin,
                string stdout,
                string stderr,
                Hashtable environmentVariables,
                Int64[] affinity
                ) : this(commandLine, workingDirectory, stdin, stdout, stderr, environmentVariables)
        {
            this.affinity = affinity;
        }

        public ProcessStartInfo(
                string commandLine,
                string workingDirectory,
                string stdin,
                string stdout,
                string stderr,
                Hashtable environmentVariables,
                Int64[] affinity,
                int taskRequeueCount
                ) : this(commandLine, workingDirectory, stdin, stdout, stderr, environmentVariables, affinity)
        {
            this.taskRequeueCount = taskRequeueCount;
        }
        #endregion // Constructors
    }
}