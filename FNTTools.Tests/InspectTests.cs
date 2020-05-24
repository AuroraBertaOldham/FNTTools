//**************************************************************************************************
// InspectTests.cs                                                                                 *
// Copyright (c) 2020 Aurora Berta-Oldham                                                          *
// This code is made available under the MIT License.                                              *
//**************************************************************************************************

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FNTTools.Tests
{
    [TestClass]
    public class InspectTests
    {
        [TestMethod]
        public void All()
        {
            var program = new Program();
            var exit = program.Run(new[] { "inspect", "TestFont.fnt", "--all" });

            Assert.AreEqual(exit, 0);
        }

        [TestMethod]
        public void Page()
        {
            var program = new Program();
            var exit = program.Run(new[] { "inspect", "TestFont.fnt", "-p 0 1 2" });

            Assert.AreEqual(exit, 0);
        }

        [TestMethod]
        public void Character()
        {
            var program = new Program();
            var exit = program.Run(new[] { "inspect", "TestFont.fnt", "-c 0 1 2" });

            Assert.AreEqual(exit, 0);
        }        
        
        [TestMethod]
        public void KerningPair()
        {
            var program = new Program();
            var exit = program.Run(new[] { "inspect", "TestFont.fnt", "-k 65 66 66 67" });

            Assert.AreEqual(exit, 0);
        }

        [TestMethod]
        public void MissingSource()
        {
            var program = new Program();
            var exit = program.Run(new[] { "inspect", "zxcv" });

            Assert.AreEqual(exit, 1);
        }

        [TestMethod]
        public void InvalidKerningPair()
        {
            var program = new Program();
            var exit = program.Run(new[] { "inspect", "TestFont.fnt", "-k", "65" });

            Assert.AreEqual(exit, 1);
        }
    }
}