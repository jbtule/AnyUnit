using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PclUnit.Style.Nunit;

namespace NunitTests
{

    [TestFixture]
    public class Setup_Success : AssertionHelper
    {
        bool actual;
        [SetUp]
        public void LalaSetup()
        {
            actual = true;
        }

        [Test]
        public void Test_Success()
        {
            Assert.True(actual);
        }


        [TearDown]
        public void LalaTearDown()
        {
            actual = false;
        }
    }


    [TestFixture]
    public class Setup_Error : AssertionHelper
    {

        [SetUp]
        public void LalaSetup()
        {
            throw new NotImplementedException("Setup Error");
        }

         [Test]
        public void Test_Error()
        {
            Assert.Fail();
        }
    }


    [TestFixture, Description(" Teardown is called")]
    public class TearDown1 : AssertionHelper
    {

        [Test]
        public void Test_Error()
        {
            Assert.Okay();
        }

        [TearDown]
        public void LalaTearDown()
        {
            throw new NotImplementedException("Teardown Error");
        }
    }

    [TestFixture, Description("Ensure Teardown is called even when setup errors")]
    public class TearDown2 : AssertionHelper
    {
        [SetUp]
        public void LalaSetup()
        {
            throw new NotImplementedException("Setup Error");
        }


        [Test]
        public void Test_Fail()
        {
            Assert.Okay();
        }

        [TearDown]
        public void LalaTearDown()
        {
            Assert.Fail();
        }
    }

    [TestFixture, Description("Ensure Teardown is called even on failure")]
    public class TearDown3 : AssertionHelper
    {

        [Test]
        public void Test_Error()
        {
            Assert.Fail();
        }

        [TearDown]
        public void LalaTearDown()
        {
            throw new NotImplementedException("Teardown Error");
        }
    }
}
