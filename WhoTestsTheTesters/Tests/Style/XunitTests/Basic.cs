using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Style.Xunit;

namespace XunitTests
{
    public class Basic:TestClass
    {

        [Fact]
        public void TestTrue_Success()
        {
            Log.Write("This is just a hardcoded true");
            Assert.True(true);
        }

        [Fact]
        public void TestFalse_Success()
        {
            Assert.False(false);
        }

        [Fact]
        public void TestFalse_Fail()
        {
            Assert.False(true, "Expected False");
        }


        [Fact]
        public void TestTrue_Fail()
        {
            Assert.True(false, "Expected True");
        }


        [Fact]
        public void Test_NoError()
        {
            var test = "lala..la..lala";
        }

        [Fact]
        public void Test_Error()
        {
           throw new NotImplementedException();
        }

        [Fact(Skip = "Skipping this test")]
        public void Test_Ignore()
        {
            throw new NotImplementedException();
        }

        [Fact(Timeout = 2000),Trait("Category", "Timeout")]
        public void TestTimeout2_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                AnyUnit.Run.Test.Sleep(100);
            }
        }

        [Fact(Timeout = 10000)]
        public void TestTimeout10_Success()
        {
            Assert.Okay();
        }

    }
}
