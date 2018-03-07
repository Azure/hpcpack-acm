//------------------------------------------------------------------------------
// <copyright file="ProcessStartInfo.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">colinw</owner>
// <securityReview name="colinw" date="1-28-06"/>
//------------------------------------------------------------------------------

#region Using directives

using System;
using System.Diagnostics;
using System.Collections;

#endregion


namespace Microsoft.HpcAcm.Services.Common
{

    [Serializable()]
    public sealed class ProcessStartInfo
    {
        #region PrivateFields
        /// <summary>
        /// the command line to execute.
        /// </summary>
        private string commandLine;

        /// <summary>
        /// Search paths for files, directories for temporary files, application-specific options, and other similar information.
        /// </summary>
        private IDictionary environmentVariables;

        /// <summary>
        /// The initial directory for the process to be started.
        /// </summary>
        private string workingDirectory;

        /// <summary>
        /// standard input to the application. can be null.
        /// </summary>
        private string stdin;

        /// <summary>
        /// standard output of the application. can be null.
        /// </summary>
        private string stdout;

        /// <summary>
        /// standard error output of the application. can be null.
        /// </summary>
        private string stderr;

        /// <summary>
        /// CPU masks for this process
        /// </summary>
        private Int64[] affinity = null;

        /// <summary>
        /// Requeue count of the application (task)
        /// </summary>
        private int taskRequeueCount = 0;

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

        #region PublicProperties

        /// <summary>
        /// the application or document to start.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return commandLine;
            }
        }

        /// <summary>
        /// Search paths for files, directories for temporary files, application-specific options, and other similar information.
        /// </summary>
        public IDictionary EnvironmentVariables
        {
            get
            {
                return environmentVariables;
            }
        }

        /// <summary>
        /// The initial directory for the process to be started.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                return workingDirectory;
            }

            set
            {
                workingDirectory = value;
            }
        }

        /// <summary>
        /// Standard input redirection file.
        /// </summary>
        public string StandardInput
        {
            get
            {
                return stdin;
            }

            set
            {
                stdin = value;
            }
        }

        /// <summary>
        /// Standard output redirection file.
        /// </summary>
        public string StandardOutput
        {
            get
            {
                return stdout;
            }

            set
            {
                stdout = value;
            }
        }

        /// <summary>
        /// Standard error output redirection file.
        /// </summary>
        public string StandardError
        {
            get
            {
                return stderr;
            }

            set
            {
                stderr = value;
            }
        }

        /// <summary>
        /// CPIs that may be used by this task on this node
        /// </summary>
        public Int64[] Affinity
        {
            get
            {
                return affinity;
            }
        }

        /// <summary>
        /// Requeue count of this task on this node
        /// </summary>
        public int TaskRequeueCount
        {
            get
            {
                return taskRequeueCount;
            }
        }
        #endregion // PublicProperties
    }
}