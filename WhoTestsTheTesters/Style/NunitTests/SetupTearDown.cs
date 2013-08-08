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


        public void Test()
        {
            Assert.True(actual);
        }


    }


    [TestFixture]
    public class Setup_Error:AssertionHelper
    {

        [SetUp]
        public void LalaSetup()
        {
            throw new NotImplementedException("Setup Fail");
        }


        public void Test()
        {
            Assert.Fail();
        }
      
       
    }


    [TestFixture]
    public class TearDown_Error : AssertionHelper
    {


        public void Test()
        {
            Assert.Okay();
        }

        [TearDown]
        public void LalaTearDown()
        {
            throw new NotImplementedException("Teardown Fail");
        }
    }


    [TestFixture]
    public class TearDown_Fail : AssertionHelper
    {


        public void Test()
        {
            Assert.Fail();
        }

        [TearDown]
        public void LalaTearDown()
        {
            throw new NotImplementedException("Teardown Fail");
        }
    }
}
