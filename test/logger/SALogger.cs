﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Security;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SALogger
{
    // This logger will derive from the Microsoft.Build.Utilities.Logger class,
    // which provides it with getters and setters for Verbosity and Parameters,
    // and a default empty Shutdown() implementation.
    public class SALogger : Logger
    {
        /// <summary>
        /// Initialize is guaranteed to be called by MSBuild at the start of the build
        /// before any events are raised.
        /// </summary>
        public override void Initialize(IEventSource eventSource)
        {
            // The name of the log file should be passed as the first item in the
            // "parameters" specification in the /logger switch.  It is required
            // to pass a log file to this logger. Other loggers may have zero or more than 
            // one parameters.
            if (null == Parameters)
            {
                throw new LoggerException("Log file was not set.");
            }
            string[] parameters = Parameters.Split(';');

            string logFile = parameters[0];
            if (String.IsNullOrEmpty(logFile))
            {
                throw new LoggerException("Log file was not set.");
            }

            if (parameters.Length > 1)
            {
                throw new LoggerException("Too many parameters passed.");
            }

            try
            {
                // Open the file
                this.streamWriter = new StreamWriter(logFile);
            }
            catch (Exception ex)
            {
                if
                (
                    ex is UnauthorizedAccessException
                    || ex is ArgumentNullException
                    || ex is PathTooLongException
                    || ex is DirectoryNotFoundException
                    || ex is NotSupportedException
                    || ex is ArgumentException
                    || ex is SecurityException
                    || ex is IOException
                )
                {
                    throw new LoggerException("Failed to create log file: " + ex.Message);
                }
                else
                {
                    // Unexpected failure
                    throw;
                }
            }
            // string directory = "C:\\Users\\MiaoHuang\\Projects\\guardian-vpn"; // directory of the git repository
            // Get the files that had changes in the last commit 
            GetLastCommitChanges();

            // For brevity, we'll only register for certain event types. Loggers can also
            // register to handle TargetStarted/Finished and other events.
            eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            // BuildErrorEventArgs adds LineNumber, ColumnNumber, File, amongst other parameters
            string line = String.Format(": ERROR {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
            WriteLineWithSenderAndMessage(line, e);
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            foreach(PSObject file in this.results)
            {
                string lastCommitFile = file.ToString().Replace("/", @"\");

                if(lastCommitFile.Contains(e.File))
                {
                    // BuildWarningEventArgs adds LineNumber, ColumnNumber, File, amongst other parameters
                    string line = String.Format(": Warning {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
                    WriteLineWithSenderAndMessage(line, e);
                }
            }
        }

        /// <summary>
        /// Write a line to the log, adding the SenderName and Message
        /// (these parameters are on all MSBuild event argument objects)
        /// </summary>
        private void WriteLineWithSenderAndMessage(string line, BuildEventArgs e)
        {
            if (0 == String.Compare(e.SenderName, "MSBuild", true /*ignore case*/))
            {
                // Well, if the sender name is MSBuild, let's leave it out for prettiness
                WriteLine(line, e);
            }
            else
            {
                WriteLine(e.SenderName + ": " + line, e);
            }
        }

        /// <summary>
        /// Just write a line to the log
        /// </summary>
        private void WriteLine(string line, BuildEventArgs e)
        {
            streamWriter.WriteLine(line + e.Message);
        }

        private void GetLastCommitChanges()
        {
            using (PowerShell powershell = PowerShell.Create())
            {
                powershell.AddScript("cd ..");
                powershell.AddScript(@"git diff-tree --no-commit-id --name-only -r HEAD");
                results = powershell.Invoke();
            }
        }

        /// <summary>
        /// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
        /// events have been raised.
        /// </summary>
        public override void Shutdown()
        {
            // Done logging, let go of the file
            streamWriter.Close();
        }

        private StreamWriter streamWriter;
        private Collection<PSObject> results;
    }
}