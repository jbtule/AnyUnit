using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PclUnit;

namespace BasicTests
{
    [TestFixture(TargetOfParameterSet = typeof(Paramed), StaticMethodOfParameterSet = "GetConstructorParamSet")]
    public class Paramed:AssertionHelper
    {
        private bool _reversableTrue;
        private bool _reversableFalse;


       public Paramed(string option)
       {
           _reversableFalse = option.Contains("_Opposite");
           _reversableTrue = !_reversableFalse;
       }


        public static IEnumerable<ParameterSet> GetConstructorParamSet(Type method)
        {
            return new List<ParameterSet>()
                       {
                           new ParameterSet("normal"),
                           new ParameterSet("_Opposite"),
                       };
        }


       [Test]
        public void TestTrue_Success()
       {

           Log.Write("This is just a hardcoded true");
           Assert.True(_reversableTrue);
       }

       [Test]
       public void TestFalse_Success()
       {
           Assert.False(_reversableFalse);
       }

       [Test]
       public void TestFalse_Fail()
       {
           Assert.False(_reversableTrue, "Expected False");
       }


       [Test]
       public void TestTrue_Fail()
       {
           Assert.True(_reversableFalse, "Expected True");
       }

       [Test]
       public void Test_Fail()
       {
           if(_reversableTrue)
              Assert.Fail("Just Fail");
           else
              Assert.Okay();
       }

       [Test]
       public bool TestReturn_Success()
       {
           return _reversableTrue;
       }


       [Test]
       public bool TestReturn_Fail()
       {
           return _reversableFalse;
       }

       [Test]
       public void Test_NoError()
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

    

       [Test(ParameterSetsTarget = typeof(Basic), ParameterSetsStaticMethod = "GetParamSet")]
       public void TestParams_Success(int i)
       {
           if (_reversableTrue)
               Assert.True(i < 10, String.Format("expected {0} to be greater than 10", i));
           else
               Assert.True(i > 10, String.Format("expected {0} to be less than 10", i));
       }

       [Test(ParameterSetsTarget = typeof(Basic), ParameterSetsStaticMethod = "GetParamSet")]
       public void TestParams_Fail(int i)
       {
           if (_reversableTrue)
                Assert.True(i > 10 , String.Format("expected {0} to be greater than 10", i));
           else
               Assert.True(i < 10, String.Format("expected {0} to be less than 10", i));
       }

       [Test(ParameterSetsTarget = typeof(Basic), ParameterSetsStaticMethod = "GetPartialParamSet")]
       public void TestParams_Partial(int i, Expected expected)
       {
           if(_reversableTrue)
               Assert.True(i > 3, String.Format("expected {0} to be greater than 3", i));
           else
               Assert.True(i <= 3, String.Format("expected {0} to be less than 3", i));
       }

    }
}
