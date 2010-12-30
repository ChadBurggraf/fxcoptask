//-----------------------------------------------------------------------
// <copyright file="ProgramLocationTests.cs" company="Tasty Codes">
//     Copyright (c) 2010 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

namespace FxCopTask.Test
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    /// <summary>
    /// Program location tests.
    /// </summary>
    [TestClass]
    public sealed class ProgramLocationTests
    {
        /// <summary>
        /// Find default tests.
        /// </summary>
        [TestMethod]
        public void ProgramLocationFindDefault()
        {
            ProgramLocation location = ProgramLocation.FindDefault();
            Assert.IsTrue(location.Found);
            Assert.AreEqual("FxCopCmd.exe", Path.GetFileName(location.Executable));
            Assert.IsTrue(0 < location.RuleAssemblies.Count());
        }
    }
}
