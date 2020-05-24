//**************************************************************************************************
// ConvertTests.cs                                                                                 *
// Copyright (c) 2020 Aurora Berta-Oldham                                                          *
// This code is made available under the MIT License.                                              *
//**************************************************************************************************

using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FNTTools.Tests
{
    [TestClass]
    public class ConvertTests
    {
        [TestMethod]
        public void ConvertWithOutput()
        {
            File.Delete("ConvertTest.fnt");

            var program = new Program();
            var exit = program.Run(new []{"convert", "xml", "TestFont.fnt", "--output", "ConvertTest.fnt" });

            Assert.AreEqual(exit, 0);
            Assert.IsTrue(File.Exists("ConvertTest.fnt"));
        }

        [TestMethod]
        public void MissingSource()
        {
            var program = new Program();
            var exit = program.Run(new[] { "convert", "xml", "asdf", "--output", "asdf2" });

            Assert.AreEqual(exit, 1);
            Assert.IsFalse(File.Exists("asdf2"));
        }       
        
        [TestMethod]
        public void InvalidFormat()
        {
            var program = new Program();
            var exit = program.Run(new[] { "convert", "hjkl", "TestFont.fnt", "--output", "ThisFileShouldNotExist" });

            Assert.AreEqual(exit, 1);
            Assert.IsFalse(File.Exists("ThisFileShouldNotExist"));
        }        
        
        [TestMethod]
        public void NoOverwriteFail()
        {
            var program = new Program();
            var exit = program.Run(new[] { "convert", "xml", "TestFont.fnt" });

            Assert.AreEqual(exit, 1);
        }        
        
        [TestMethod]
        public void Overwrite()
        {
            File.Copy("TestFont.fnt", "OverwriteTest.fnt", true);

            var program = new Program();
            var exit = program.Run(new[] { "convert", "xml", "OverwriteTest.fnt", "--overwrite" });

            Assert.AreEqual(exit, 0);
        }
    }
}
