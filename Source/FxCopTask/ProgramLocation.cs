//-----------------------------------------------------------------------
// <copyright file="ProgramLocation.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace FxCopTask
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Represents the location on disk of the FxCop installation.
    /// </summary>
    public sealed class ProgramLocation
    {
        private bool found;
        private string rulesDirectory;
        private IEnumerable<string> ruleAssemblies;

        /// <summary>
        /// Gets or sets the path to the FxCopCmd executable.
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the location was found.
        /// </summary>
        public bool Found
        {
            get
            {
                return this.found;
            }

            set
            {
                this.found = value;
                this.ruleAssemblies = null;
            }
        }

        /// <summary>
        /// Gets a collection of rule assemblies from this instance's <see cref="RulesDirectory"/>.
        /// </summary>
        public IEnumerable<string> RuleAssemblies
        {
            get
            {
                if (this.ruleAssemblies == null)
                {
                    if (this.Found && !String.IsNullOrEmpty(this.RulesDirectory) && Directory.Exists(this.RulesDirectory))
                    {
                        this.ruleAssemblies = Directory.GetFiles(this.RulesDirectory, "*.dll");
                    }
                    else
                    {
                        this.ruleAssemblies = new string[0];
                    }
                }

                return this.ruleAssemblies;
            }
        }

        /// <summary>
        /// Gets or sets the path to the rule assembly directory.
        /// </summary>
        public string RulesDirectory
        {
            get
            {
                return this.rulesDirectory;
            }

            set
            {
                this.rulesDirectory = value;
                this.ruleAssemblies = null;
            }
        }

        /// <summary>
        /// Gets or sets the path to the ruleset directory, if one was found.
        /// </summary>
        public string RuleSetDirectory { get; set; }

        /// <summary>
        /// Finds FxCop in the default location or in the location installed by Visual Studio 2010.
        /// </summary>
        /// <returns>The FxCop location.</returns>
        public static ProgramLocation FindDefault()
        {
            string executable = null, rulesDirectory = null, ruleSetDirectory = null;

            bool found = FindVisualStudio("VS120COMNTOOLS", out executable, out rulesDirectory, out ruleSetDirectory)
                || FindVisualStudio("VS110COMNTOOLS", out executable, out rulesDirectory, out ruleSetDirectory)
                || FindVisualStudio("VS100COMNTOOLS", out executable, out rulesDirectory, out ruleSetDirectory);

            if (!found)
            {
                string prog86Path = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

                if (!String.IsNullOrEmpty(prog86Path))
                {
                    executable = Path.Combine(prog86Path, @"Microsoft Fxcop 10.0\FxCopCmd.exe");
                    rulesDirectory = Path.Combine(prog86Path, @"Microsoft Fxcop 10.0\Rules");
                    found = File.Exists(executable) && Directory.Exists(rulesDirectory);
                }
            }

            if (!found)
            {
                string progPath = Environment.GetEnvironmentVariable("PROGRAMFILES");

                if (!String.IsNullOrEmpty(progPath))
                {
                    executable = Path.Combine(progPath, @"Microsoft Fxcop 10.0\FxCopCmd.exe");
                    rulesDirectory = Path.Combine(progPath, @"Microsoft Fxcop 10.0\Rules");
                    found = File.Exists(executable) && Directory.Exists(rulesDirectory);
                }
            }

            return new ProgramLocation()
            {
                Executable = found ? Path.GetFullPath(executable) : null,
                Found = found,
                RulesDirectory = found ? Path.GetFullPath(rulesDirectory) : null,
                RuleSetDirectory = found && !String.IsNullOrEmpty(ruleSetDirectory) && Directory.Exists(ruleSetDirectory) ? Path.GetFullPath(ruleSetDirectory) : null
            };
        }

        private static bool FindVisualStudio(string env, out string executable, out string rulesDirectory, out string ruleSetDirectory)
        {
            executable = null;
            rulesDirectory = null; 
            ruleSetDirectory = null;
            bool found = false;
            string visualStudioToolsPath = Environment.GetEnvironmentVariable(env);

            if (!String.IsNullOrEmpty(visualStudioToolsPath))
            {
                executable = Path.Combine(visualStudioToolsPath, @"..\..\Team Tools\Static Analysis Tools\FxCop\FxCopCmd.exe");
                rulesDirectory = Path.Combine(visualStudioToolsPath, @"..\..\Team Tools\Static Analysis Tools\FxCop\Rules");
                ruleSetDirectory = Path.Combine(visualStudioToolsPath, @"..\..\Team Tools\Static Analysis Tools\Rule Sets");
                found = File.Exists(executable) && Directory.Exists(rulesDirectory);
            }

            return found;
        }
    }
}
