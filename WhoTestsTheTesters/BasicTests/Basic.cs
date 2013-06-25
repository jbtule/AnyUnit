using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PclUnit;

namespace BasicTests
{
    public enum Expected
    {
        _Error,
        _Success,
        _Ignore,
        _Fail,
        _NoError
    }


    [TestFixture]
    public class Basic:AssertionHelper
    {
        
        [Test]
        public void TestTrue_Success()
        {

            Log.Write("This is just a hardcoded true");
            Assert.True(true);
        }

        [Test]
        public void TestFalse_Success()
        {
            Assert.False(false);
        }

        [Test]
        public void TestFalse_Fail()
        {
            Assert.False(true, "Expected False");
        }


        [Test]
        public void TestTrue_Fail()
        {
            Assert.True(false, "Expected True");
        }

        [Test]
        public void TestFail_Fail()
        {
            Assert.Fail("Just Fail");
        }

        [Test]
        public bool TestReturn_Success()
        {
            return true;
        }


        [Test]
        public bool TestReturn_Fail()
        {
            return false;
        }
             
        [Test]
        public void TestNothing_NoError()
        {
        }

        [Test]
        public void Test_Error()
        {

            throw new NotSupportedException("This should be an error.");
        }

        [Test]
        public void Test_Ignore()
        {
            Assert.Ignore("Ignoring...");
        }

        public static IEnumerable<object[]> GetParamSet(MethodInfo method)
        {
            return Enumerable.Range(1, 5).Select(it => new object[] { it });
        }

        [Test(StaticMethodOfParameterSet = "GetParamSet")]
        public void TestParams_Success(int i)
        {
            Assert.True(i < 10, "You shouldn't seee this message");
        }

        [Test(StaticMethodOfParameterSet = "GetParamSet")]
        public void TestParams_Fail(int i)
        {
            Assert.True(i > 10, String.Format("expected {0} to be greater than 10",i));
        }

     
        public static IEnumerable<object[]> GetPartialParamSet(MethodInfo method)
        {
            return new List<object[]>
                       {
                           new object[] {1,Expected._Fail},
                           new object[] {2,Expected._Fail},
                           new object[] {3,Expected._Fail},
                           new object[] {4,Expected._Success},
                           new object[] {5,Expected._Success},
                       };
        }
        [Test(StaticMethodOfParameterSet = "GetPartialParamSet")]
        public void TestParams_Partial(int i, Expected expected)
        {
            Assert.True(i > 3, String.Format("expected {0} to be greater than 3", i));
        }
    }
}
