//-----------------------------------------------------------------------
// <copyright file="OutputParserTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace FxCopTask.Test
{
    using System;
    using System.Xml.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Output parser tests.
    /// </summary>
    [TestClass]
    public sealed class OutputParserTests
    {
        /// <summary>
        /// Get error code tests.
        /// </summary>
        [TestMethod]
        public void OutputParserGetErrorCode()
        {
            XElement message = XElement.Parse(@"<Message TypeName=""DoNotIndirectlyExposeMethodsWithLinkDemands"" Category=""Microsoft.Security"" CheckId=""CA2122"" Status=""Active"" Created=""2010-12-29 19:57:09Z"" FixCategory=""NonBreaking""/>");
            Assert.AreEqual("CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", OutputParser.GetErrorCode(message));
        }

        /// <summary>
        /// Get file name tests.
        /// </summary>
        [TestMethod]
        public void OutputParserGetFileName()
        {
            XElement issue = XElement.Parse(@"<Issue Certainty=""95"" Level=""Error"">Mark 'FxCopTask.dll' with CLSCompliant(true) because it exposes externally visible types.</Issue>");
            Assert.AreEqual("FxCopTask.dll", OutputParser.GetFileName("$(ProjectDir)/FxCopTask/bin/Debug/FxCopTask.dll", issue));
            issue = XElement.Parse(@"<Issue Certainty=""33"" Level=""CriticalError"" Path=""C:\Projects\FxCopTask\FxCopTask"" File=""FxCop.cs"" Line=""176"">'FxCop.CreateProcess(string)' calls into 'Process.StartInfo.set(ProcessStartInfo)' which has a LinkDemand. By making this call, 'Process.StartInfo.set(ProcessStartInfo)' is indirectly exposed to user code. Review the following call stack that might expose a way to circumvent security protection: &#xD;&#xA;   -&gt;'FxCop.CreateProcess(string)'&#xD;&#xA;   -&gt;'FxCop.CreateProcess(string)'&#xD;&#xA;   -&gt;'FxCop.Execute()'</Issue>");
            Assert.AreEqual(@"C:\Projects\FxCopTask\FxCopTask\FxCop.cs", OutputParser.GetFileName("$(ProjectDir)/FxCopTask/bin/Debug/FxCopTask.dll", issue));
        }

        /// <summary>
        /// Gets level tests.
        /// </summary>
        [TestMethod]
        public void OutputParserGetLevel()
        {
            XElement issue = XElement.Parse(@"<Issue Certainty=""95"" Level=""Error"">Mark 'FxCopTask.dll' with CLSCompliant(true) because it exposes externally visible types.</Issue>");
            Assert.AreEqual("Error", OutputParser.GetLevel(issue));
        }

        /// <summary>
        /// Gets line number tests.
        /// </summary>
        [TestMethod]
        public void OutputParserGetLineNumber()
        {
            XElement issue = XElement.Parse(@"<Issue Certainty=""95"" Level=""Error"">Mark 'FxCopTask.dll' with CLSCompliant(true) because it exposes externally visible types.</Issue>");
            Assert.AreEqual(0, OutputParser.GetLineNumber(issue));
            issue = XElement.Parse(@"<Issue Certainty=""33"" Level=""CriticalError"" Path=""C:\Projects\FxCopTask\FxCopTask"" File=""FxCop.cs"" Line=""176"">'FxCop.CreateProcess(string)' calls into 'Process.StartInfo.set(ProcessStartInfo)' which has a LinkDemand. By making this call, 'Process.StartInfo.set(ProcessStartInfo)' is indirectly exposed to user code. Review the following call stack that might expose a way to circumvent security protection: &#xD;&#xA;   -&gt;'FxCop.CreateProcess(string)'&#xD;&#xA;   -&gt;'FxCop.CreateProcess(string)'&#xD;&#xA;   -&gt;'FxCop.Execute()'</Issue>");
            Assert.AreEqual(176, OutputParser.GetLineNumber(issue));
        }

        /// <summary>
        /// Gets message tests.
        /// </summary>
        [TestMethod]
        public void OutputParserGetMessage()
        {
            XElement issue = XElement.Parse(@"<Issue Certainty=""95"" Level=""Error"">Mark 'FxCopTask.dll' with CLSCompliant(true) because it exposes externally visible types.</Issue>");
            Assert.AreEqual("Mark 'FxCopTask.dll' with CLSCompliant(true) because it exposes externally visible types.", OutputParser.GetMessage(issue));
            issue = XElement.Parse(@"<Issue Certainty=""33"" Level=""CriticalError"" Path=""C:\Projects\FxCopTask\FxCopTask"" File=""FxCop.cs"" Line=""176"">'FxCop.CreateProcess(string)' calls into 'Process.StartInfo.set(ProcessStartInfo)' which has a LinkDemand. By making this call, 'Process.StartInfo.set(ProcessStartInfo)' is indirectly exposed to user code. Review the following call stack that might expose a way to circumvent security protection: &#xD;&#xA;   -&gt;'FxCop.CreateProcess(string)'&#xD;&#xA;   -&gt;'FxCop.CreateProcess(string)'&#xD;&#xA;   -&gt;'FxCop.Execute()'</Issue>");
            Assert.AreEqual("'FxCop.CreateProcess(string)' calls into 'Process.StartInfo.set(ProcessStartInfo)' which has a LinkDemand. By making this call, 'Process.StartInfo.set(ProcessStartInfo)' is indirectly exposed to user code. Review the following call stack that might expose a way to circumvent security protection: \n   ->'FxCop.CreateProcess(string)'\n   ->'FxCop.CreateProcess(string)'\n   ->'FxCop.Execute()'", OutputParser.GetMessage(issue));
        }

        /// <summary>
        /// Parse tests.
        /// </summary>
        [TestMethod]
        public void OutputParserParse()
        {
            int errorCount = 0, warningCount = 0;
            Logger errorLogger = (errorCode, file, lineNumber, message) => errorCount++;
            Logger warningLogger = (errorCode, file, lineNumber, message) => warningCount++;
            OutputParser parser = new OutputParser("FxCopTask.xml", errorLogger, warningLogger);
            Assert.IsFalse(parser.Parse(true, false));
            Assert.AreEqual(15, errorCount);
            Assert.AreEqual(0, warningCount);
        }
    }
}
