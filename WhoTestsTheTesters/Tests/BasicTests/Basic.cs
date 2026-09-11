using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using AnyUnit;
using AnyUnit.Run;

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


        [Test(Timeout = 1000, Category = "Timeout")]
        public void TestTimeout_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay(); 
                Test.Sleep(100);
            }
        }

        [Test(Timeout = 2000,Category = "Timeout")]
        public void TestTimeout2_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }
        [Test(Timeout = 3000, Category = "Timeout")]
        public void TestTimeout3_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }

        [Test(Timeout = 10000)]
        public void TestTimeout10_Success()
        {
            Assert.Okay();
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

        public static IEnumerable<ParameterSet> GetParamSet(MethodInfo method)
        {
            return Enumerable.Range(1, 5).Select(it => new ParameterSet(it));
        }

        [Test(ParameterMethod = "GetParamSet")]
        public void TestParams_Success(int i)
        {
            Assert.True(i < 10, "You shouldn't seee this message");
        }

        [Test(ParameterMethod = "GetParamSet")]
        public void TestParams_Fail(int i)
        {
            Assert.True(i > 10, String.Format("expected {0} to be greater than 10",i));
        }


        public static IEnumerable<ParameterSet> GetPartialParamSet(MethodInfo method)
        {
            return new List<ParameterSet>
                       {
                           new ParameterSet(1,Expected._Fail),
                           new ParameterSet(2,Expected._Fail),
                           new ParameterSet(3,Expected._Fail),
                           new ParameterSet(4,Expected._Success),
                           new ParameterSet(5,Expected._Success),
                       };
        }
        [Test(ParameterMethod = "GetPartialParamSet")]
        public void TestParams_Partial(int i, Expected expected)
        {
            Assert.True(i > 3, String.Format("expected {0} to be greater than 3", i));
        }
    }
}
