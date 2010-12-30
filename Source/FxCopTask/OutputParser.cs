//-----------------------------------------------------------------------
// <copyright file="OutputParser.cs" company="Tasty Codes">
//     Original copyright (c) 2010 Brad Wilson.
//     The original can be found at http://bradwilson.typepad.com/blog/2010/04/writing-an-fxcop-task-for-msbuild.html
//     Modifications copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace FxCopTask
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Web;
    using System.Xml.Linq;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Function definition for logging messages found while parsing FxCop XML output.
    /// </summary>
    /// <param name="errorCode">The error code of the message to log.</param>
    /// <param name="file">The file the message relates to.</param>
    /// <param name="lineNumber">The line number in the file the message relates to.</param>
    /// <param name="message">The message to log.</param>
    public delegate void Logger(string errorCode, string file, int lineNumber, string message);

    /// <summary>
    /// Parses FxCop output XML.
    /// </summary>
    public sealed class OutputParser
    {
        /// <summary>
        /// Initializes a new instance of the OutputParser class.
        /// </summary>
        /// <param name="outputPath">The path of the output file to parse.</param>
        /// <param name="errorLogger">The logging function to use when logging error messages.</param>
        /// <param name="warningLogger">The logging function to use when logging warning messages.</param>
        public OutputParser(string outputPath, Logger errorLogger, Logger warningLogger)
        {
            if (String.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException("outputPath", "outputPath must contain a value.");
            }

            if (errorLogger == null)
            {
                throw new ArgumentNullException("errorLogger", "errorLogger cannot be null.");
            }

            if (warningLogger == null)
            {
                throw new ArgumentNullException("warningLogger", "warningLogger cannot be null.");
            }

            this.OutputPath = outputPath;
            this.ErrorLogger = errorLogger;
            this.WarningLogger = warningLogger;
        }

        /// <summary>
        /// Gets the logging function to use when logging error messages.
        /// </summary>
        public Logger ErrorLogger { get; private set; }

        /// <summary>
        /// Gets the path of the output file to parse.
        /// </summary>
        public string OutputPath { get; private set; }

        /// <summary>
        /// Gets the logging function to use when logging warning messages.
        /// </summary>
        public Logger WarningLogger { get; private set; }

        /// <summary>
        /// Gets the error code from the given message XML element.
        /// </summary>
        /// <param name="message">The message XML element to get the error code from.</param>
        /// <returns>The message's error code.</returns>
        public static string GetErrorCode(XElement message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message", "message cannot be null.");
            }

            return String.Concat((string)message.Attribute("CheckId"), ":", (string)message.Attribute("TypeName"));
        }

        /// <summary>
        /// Gets the file name from the given issue, defaulting to the given assembly path if none was found.
        /// </summary>
        /// <param name="assembly">The path of the assembly the issue belongs to.</param>
        /// <param name="issue">The issue XML element to get the file name from.</param>
        /// <returns>The issue's file name.</returns>
        public static string GetFileName(string assembly, XElement issue)
        {
            if (String.IsNullOrEmpty(assembly))
            {
                throw new ArgumentNullException("assembly", "assembly must contain a value.");
            }

            if (issue == null)
            {
                throw new ArgumentNullException("issue", "issue cannot be null.");
            }

            string result;
            string path = (string)issue.Attribute("Path");
            string file = (string)issue.Attribute("File");

            if (!String.IsNullOrEmpty(path) && !String.IsNullOrEmpty(file))
            {
                result = Path.Combine(path, file);
            }
            else
            {
                result = Path.GetFileName(assembly);
            }

            return FxCop.GetRelativePath(result);
        }

        /// <summary>
        /// Gets the level from the given issue XML element.
        /// </summary>
        /// <param name="issue">The issue XML element to get the level from.</param>
        /// <returns>The issue's level.</returns>
        public static string GetLevel(XElement issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException("issue", "issue cannot be null.");
            }

            return (string)issue.Attribute("Level");
        }

        /// <summary>
        /// Gets the line number from the given issue XML element.
        /// </summary>
        /// <param name="issue">The issue XML element to get the line number from.</param>
        /// <returns>The issue's line number.</returns>
        public static int GetLineNumber(XElement issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException("issue", "issue cannot be null.");
            }

            string line = (string)issue.Attribute("Line");
            int result = 0;

            if (!String.IsNullOrEmpty(line))
            {
                result = Int32.Parse(line, CultureInfo.InvariantCulture);
            }

            return result;
        }

        /// <summary>
        /// Gets the message from the given issue XML element.
        /// </summary>
        /// <param name="issue">The issue XML element to get the message from.</param>
        /// <returns>The issue's message.</returns>
        public static string GetMessage(XContainer issue)
        {
            if (issue == null)
            {
                throw new ArgumentNullException("issue", "issue cannot be null.");
            }

            return HttpUtility.HtmlDecode((string)issue.FirstNode.ToString().Replace("\r\n", "\n"));
        }

        /// <summary>
        /// Parses this instance's output file.
        /// </summary>
        /// <param name="failOnError">A value indicating whether to return failure when an error has been encountered.</param>
        /// <param name="failOnWarning">A value indicating whether to return failure when a warning has been encountered.</param>
        /// <returns>An indication of success or failure, depending on the failure settings passed during the call.</returns>
        public bool Parse(bool failOnError, bool failOnWarning)
        {
            bool hasErrors = false;
            bool hasWarnings = false;
            XElement root = XElement.Load(this.OutputPath);

            foreach (XElement target in root.Descendants("Target"))
            {
                string assembly = (string)target.Attribute("Name");

                foreach (XElement message in target.Descendants("Message"))
                {
                    string errorCode = GetErrorCode(message);

                    foreach (XElement issue in message.Descendants("Issue"))
                    {
                        string level = GetLevel(issue).ToUpperInvariant();
                        Logger logger;

                        if (level.EndsWith("ERROR", StringComparison.Ordinal))
                        {
                            logger = failOnError ? this.ErrorLogger : this.WarningLogger;
                            hasErrors = true;
                        }
                        else if (level.EndsWith("WARNING", StringComparison.Ordinal))
                        {
                            logger = failOnWarning ? this.ErrorLogger : this.WarningLogger;
                            hasWarnings = true;
                        }
                        else
                        {
                            logger = this.WarningLogger;
                        }

                        logger(errorCode, GetFileName(assembly, issue), GetLineNumber(issue), GetMessage(issue));
                    }
                }
            }

            return !((hasErrors && failOnError) || (hasWarnings && failOnWarning));
        }
    }
}
