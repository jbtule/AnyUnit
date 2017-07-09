using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit.Style.Nunit;

namespace NunitTests
{
    [TestFixture("normal")]
    [TestFixture("_Opposite")]
    public class Paramed:AssertionHelper
    {
        private bool _reversableTrue;
        private bool _reversableFalse;


       public Paramed(string option)
       {
           _reversableFalse = option.Contains("_Opposite");
           _reversableTrue = !_reversableFalse;
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
       public void TestParams1([Range(1, 5)]int i, [Values(Expected._Success)] Expected expected)
       {
           if (_reversableTrue)
               Assert.True(i < 10, String.Format("expected {0} to be greater than 10", i));
           else
               Assert.True(i > 10, String.Format("expected {0} to be less than 10", i));
       }
          
       [Test]
       public void TestParams2([Range(1, 5)]int i, [Values(Expected._Fail)] Expected expected)
       {
           if (_reversableTrue)
               Assert.True(i > 10 , String.Format("expected {0} to be greater than 10", i));
           else
               Assert.True(i < 10, String.Format("expected {0} to be less than 10", i));
       }

       [TestCase(1, Expected._Fail)]
       [TestCase(2, Expected._Fail)]
       [TestCase(3, Expected._Fail)]
       [TestCase(4, Expected._Success)]
       [TestCase(5, Expected._Success)]
       public void TestParams_Partial(int i, Expected expected)
       {
           if(_reversableTrue)
               Assert.True(i > 3, String.Format("expected {0} to be greater than 3", i));
           else
               Assert.True(i <= 3, String.Format("expected {0} to be less than 3", i));
       }

    }
}
