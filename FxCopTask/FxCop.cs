//-----------------------------------------------------------------------
// <copyright file="FxCop.cs" company="Tasty Codes">
//     Original copyright (c) 2010 Brad Wilson.
//     The original can be found at http://bradwilson.typepad.com/blog/2010/04/writing-an-fxcop-task-for-msbuild.html
//     Modifications copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace FxCopTask
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Executes FxCop from MSBuild.
    /// </summary>
    public sealed class FxCop : Task
    {
        /// <summary>
        /// Initializes a new instance of the FxCop class.
        /// </summary>
        public FxCop()
        {
            this.FailOnError = true;
            this.FailOnWarning = false;
            this.RuleSet = "AllRules.ruleset";

            ProgramLocation location = ProgramLocation.FindDefault();

            if (location.Found)
            {
                this.Executable = new TaskItem(location.Executable);
                this.RuleSetDirectory = new TaskItem(location.RuleSetDirectory);
            }
        }

        /// <summary>
        /// Gets or sets the collection of assemblies to run FxCop against.
        /// </summary>
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        /// <summary>
        /// Gets or sets the path custom dictionary to pass to FxCop.
        /// </summary>
        public ITaskItem Dictionary { get; set; }

        /// <summary>
        /// Gets or sets the pass of FxCopCmd.exe.
        /// </summary>
        public ITaskItem Executable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to fail the build on FxCop errors.
        /// </summary>
        public bool FailOnError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to fail the build on FxCop warnings.
        /// </summary>
        public bool FailOnWarning { get; set; }

        /// <summary>
        /// Gets or sets the path to save the FxCop output to.
        /// </summary>
        public ITaskItem Output { get; set; }

        /// <summary>
        /// Gets or sets the name of the rulset to use.
        /// </summary>
        public string RuleSet { get; set; }

        /// <summary>
        /// Gets or sets the path to the ruleset directory.
        /// </summary>
        public ITaskItem RuleSetDirectory { get; set; }

        /// <summary>
        /// Gets the relative path of the given path string, compared to the currently environment directory.
        /// </summary>
        /// <param name="path">The path to get the relative path of.</param>
        /// <returns>A relative path.</returns>
        public static string GetRelativePath(string path)
        {
            string currentDirectory = Directory.GetCurrentDirectory().ToUpperInvariant().TrimEnd('\\') + @"\";

            if (path.ToUpperInvariant().StartsWith(currentDirectory, StringComparison.Ordinal))
            {
                path = path.Substring(currentDirectory.Length);
            }

            return path;
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>True if the task succeeded, false otherwise.</returns>
        public override bool Execute()
        {
            bool success = false;
            bool deleteOutput = false;

            if (this.Executable == null)
            {
                Log.LogError("Executable was not set, and FxCop could not be found in any standard installation locations.");
            }
            else
            {
                if (this.RuleSetDirectory == null)
                {
                    Log.LogError("RuleSetDirectory was not set, and FxCop could not be found in any standard installation locations.");
                }
                else
                {
                    try
                    {
                        if (this.Output == null)
                        {
                            this.Output = new TaskItem(Path.GetTempFileName());
                            deleteOutput = true;
                        }

                        StringBuilder assemblyArgs = new StringBuilder();

                        foreach (ITaskItem assembly in this.Assemblies)
                        {
                            string fullPath = assembly.GetMetadata("FullPath");
                            assemblyArgs.AppendFormat(CultureInfo.InvariantCulture, @" /f:""{0}""", fullPath);
                            Log.LogMessage(MessageImportance.High, "Running FxCop on '{0}'.", GetRelativePath(fullPath));
                        }

                        using (Process process = this.CreateProcess(assemblyArgs.ToString()))
                        {
                            if (process.Start()) 
                            {
                                process.WaitForExit();

                                if (process.ExitCode == 0)
                                {
                                    Logger errorLogger = (errorCode, file, lineNumber, message) => Log.LogError("FxCop", errorCode, null, file, lineNumber, 0, 0, 0, message);
                                    Logger warningLogger = (errorCode, file, lineNumber, message) => Log.LogWarning("FxCop", errorCode, null, file, lineNumber, 0, 0, 0, message);
                                    OutputParser parser = new OutputParser(this.Output.GetMetadata("FullPath"), errorLogger, warningLogger);

                                    success = parser.Parse(this.FailOnError, this.FailOnWarning);
                                }
                                else
                                {
                                    this.LogExecutionError(process);
                                }
                            }
                            else 
                            {
                                Log.LogError("Failed to start the FxCop process at '{0}'.", this.Executable.GetMetadata("FullPath"));
                            }
                        }
                    }
                    finally
                    {
                        if (deleteOutput && File.Exists(this.Output.GetMetadata("FullPath")))
                        {
                            File.Delete(this.Output.GetMetadata("FullPath"));
                        }
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Creates an FxCop process, passing the given prepared assembly arguments.
        /// </summary>
        /// <param name="assemblyArgs">A string of prepared assembly arguments.</param>
        /// <returns>The created process.</returns>
        private Process CreateProcess(string assemblyArgs)
        {
            string arguments = String.Format(
                CultureInfo.InvariantCulture,
                @"/q /iit /fo /igc /gac /rs:""={0}"" /rsd:""{1}"" /o:""{2}""",
                this.RuleSet,
                this.RuleSetDirectory.GetMetadata("FullPath"),
                this.Output.GetMetadata("FullPath")
            );

            if (this.Dictionary != null)
            {
                arguments += String.Format(CultureInfo.InvariantCulture, @" /dic:""{0}""", Dictionary.GetMetadata("FullPath"));
            }
            
            ProcessStartInfo start = new ProcessStartInfo(this.Executable.GetMetadata("FullPath"), arguments + assemblyArgs)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            return new Process() { StartInfo = start };
        }

        /// <summary>
        /// Logs a process execution error for the given process to the build engine.
        /// </summary>
        /// <param name="process">The process to log the execution error for.</param>
        private void LogExecutionError(Process process)
        {
            string line;

            while (null != (line = process.StandardOutput.ReadLine()))
            {
                Log.LogMessage(MessageImportance.High, line);
            }

            while (null != (line = process.StandardError.ReadLine()))
            {
                Log.LogMessage(MessageImportance.High, line);
            }

            Log.LogError("FxCop exited with code {0}.", process.ExitCode);
        }
    }
}
