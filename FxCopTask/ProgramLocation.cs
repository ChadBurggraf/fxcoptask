//-----------------------------------------------------------------------
// <copyright file="ProgramLocation.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace FxCopTask
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents the location on disk of the FxCop installation.
    /// </summary>
    public sealed class ProgramLocation
    {
        /// <summary>
        /// Gets or sets the path to the FxCopCmd executable.
        /// </summary>
        public string Executable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the location was found.
        /// </summary>
        public bool Found { get; set; }

        /// <summary>
        /// Gets or sets the path to the rule set directory.
        /// </summary>
        public string RuleSetDirectory { get; set; }

        /// <summary>
        /// Finds FxCop in the default location or in the location installed by Visual Studio 2010.
        /// </summary>
        /// <returns>The FxCop location.</returns>
        public static ProgramLocation FindDefault()
        {
            bool found = false;
            string executable = null, ruleSetDirectory = null;
            string vs10ToolsPath = Environment.GetEnvironmentVariable("VS100COMNTOOLS");

            if (!String.IsNullOrEmpty(vs10ToolsPath))
            {
                executable = Path.Combine(vs10ToolsPath, @"..\..\Team Tools\Static Analysis Tools\FxCop\FxCopCmd.exe");
                ruleSetDirectory = Path.Combine(vs10ToolsPath, @"..\..\Team Tools\Static Analysis Tools\Rule Sets");
                found = File.Exists(executable) && Directory.Exists(ruleSetDirectory);
            }

            if (!found)
            {
                string prog86Path = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

                if (!String.IsNullOrEmpty(prog86Path))
                {
                    executable = Path.Combine(prog86Path, @"Microsoft Fxcop 10.0\FxCopCmd.exe");
                    ruleSetDirectory = Path.Combine(prog86Path, @"Microsoft Fxcop 10.0\Rules");
                    found = File.Exists(executable) && Directory.Exists(ruleSetDirectory);
                }
            }

            if (!found)
            {
                string progPath = Environment.GetEnvironmentVariable("PROGRAMFILES");

                if (!String.IsNullOrEmpty(progPath))
                {
                    executable = Path.Combine(progPath, @"Microsoft Fxcop 10.0\FxCopCmd.exe");
                    ruleSetDirectory = Path.Combine(progPath, @"Microsoft Fxcop 10.0\Rules");
                    found = File.Exists(executable) && Directory.Exists(ruleSetDirectory);
                }
            }

            return new ProgramLocation()
            {
                Executable = found ? executable : null,
                Found = found,
                RuleSetDirectory = found ? ruleSetDirectory : null
            };
        }
    }
}
