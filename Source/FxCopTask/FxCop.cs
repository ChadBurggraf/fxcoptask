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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Permissions;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Executes FxCop from MSBuild.
    /// </summary>
    [HostProtectionAttribute(SecurityAction.LinkDemand, SharedState = true, Synchronization = true, ExternalProcessMgmt = true, SelfAffectingProcessMgmt = true)]
    public sealed class FxCop : Task
    {
        /// <summary>
        /// Initializes a new instance of the FxCop class.
        /// </summary>
        public FxCop()
        {
            this.FailOnError = true;
            this.FailOnWarning = false;

            ProgramLocation location = ProgramLocation.FindDefault();

            if (location.Found)
            {
                this.Executable = new TaskItem(location.Executable);

                if (!String.IsNullOrEmpty(location.RuleSetDirectory))
                {
                    this.RuleSet = "AllRules.ruleset";
                    this.RuleSetDirectory = new TaskItem(location.RuleSetDirectory);
                }
                else
                {
                    this.Rules = location.RuleAssemblies.Select(p => new TaskItem(p)).ToArray();
                }
            }
        }

        /// <summary>
        /// Gets or sets the collection of assemblies to run FxCop against.
        /// </summary>
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        /// <summary>
        /// Gets or sets the path of the custom dictionary to pass to FxCop.
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
        /// Gets or sets a collection of rule assemblies to use when running FxCop.
        /// </summary>
        public ITaskItem[] Rules { get; set; }

        /// <summary>
        /// Gets or sets the rule set to use, when not using rule assemblies.
        /// </summary>
        public string RuleSet { get; set; }

        /// <summary>
        /// Gets or sets the rule set directory to use, when not using rule assemblies.
        /// </summary>
        public ITaskItem RuleSetDirectory { get; private set; }

        /// <summary>
        /// Gets the relative path of the given path string, compared to the currently environment directory.
        /// </summary>
        /// <param name="path">The path to get the relative path of.</param>
        /// <returns>A relative path.</returns>
        public static string GetRelativePath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path", "path must contain a value.");
            }

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
        [PermissionSetAttribute(SecurityAction.Demand, Name = "FullTrust")]
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Justification = "There is no way I'm globalizing this.")]
        [SuppressMessage("Microsoft.Naming", "CA2204:LiteralsShouldBeSpelledCorrectly", Justification = "It's funny that FxCop is not in the dictionary.")]
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
                if ((this.Rules == null || this.Rules.Length == 0) && (String.IsNullOrEmpty(this.RuleSet) || this.RuleSetDirectory == null))
                {
                    Log.LogError("Either RuleSet and RuleSetDirectory must be set, or Rules must be set. FxCop could not be found in any standard installation locations to set these values automatically.");
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
                        else
                        {
                            string directory = Path.GetDirectoryName(this.Output.GetMetadata("FullPath"));

                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }
                        }

                        StringBuilder assemblyArgs = new StringBuilder();
                        Log.LogMessage(MessageImportance.High, "FxCop location: '{0}'.", this.Executable.GetMetadata("FullPath"));

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

                                Logger errorLogger = (errorCode, file, lineNumber, message) => Log.LogError("FxCop", errorCode, null, file, lineNumber, 0, 0, 0, message);
                                Logger warningLogger = (errorCode, file, lineNumber, message) => Log.LogWarning("FxCop", errorCode, null, file, lineNumber, 0, 0, 0, message);
                                OutputParser parser = new OutputParser(this.Output.GetMetadata("FullPath"), errorLogger, warningLogger);

                                success = parser.Parse(this.FailOnError, this.FailOnWarning);
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
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", Justification = "There is no way I'm globalizing this.")]
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "The object is disposed by the caller of this method.")]
        private Process CreateProcess(string assemblyArgs)
        {
            StringBuilder args = new StringBuilder();
            args.AppendFormat(
                CultureInfo.InvariantCulture,
                @"/q /iit /fo /igc /gac /o:""{0}"" {1}",
                this.Output.GetMetadata("FullPath"),
                assemblyArgs.TrimStart());

            if (!String.IsNullOrEmpty(this.RuleSet) && this.RuleSetDirectory != null)
            {
                args.AppendFormat(
                    CultureInfo.InvariantCulture, 
                    @" /rs:""={0}"" /rsd:""{1}""", 
                    this.RuleSet, 
                    Regex.Replace(this.RuleSetDirectory.GetMetadata("FullPath"), @"\\$", String.Empty));
            }
            else
            {
                foreach (ITaskItem rule in this.Rules)
                {
                    args.AppendFormat(CultureInfo.InvariantCulture, @" /r:""{0}""", rule.GetMetadata("FullPath"));
                }
            }

            if (this.Dictionary != null)
            {
                args.AppendFormat(CultureInfo.InvariantCulture, @" /dic:""{0}""", this.Dictionary.GetMetadata("FullPath"));
            }

            Log.LogMessage("FxCop arguments: '{0}'", args);

            ProcessStartInfo start = new ProcessStartInfo(this.Executable.GetMetadata("FullPath"), args.ToString())
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            return new Process() { StartInfo = start };
        }
    }
}
