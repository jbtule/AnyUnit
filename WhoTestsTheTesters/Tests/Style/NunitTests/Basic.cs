using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;
using AnyUnit.Style.Nunit;

namespace NunitTests
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
    public class Basic : AssertionHelper
    {
        [Test]
        public void TestIsTrue_Success()
        {
            Assert.True(true);
        }

        [Test]
        public void TestIsTrue_Fail()
        {
            Assert.True(false);
        }

        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test, Category("Timeout"), Timeout(1000)]
        public void TestTimeout_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }

        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test, Category("Timeout"), Timeout(2000)]
        public void TestTimeout2_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }

        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test, Category("Timeout"), Timeout(3000)]
        public void TestTimeout3_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }

        [Test, Timeout(3000)]
        public void TestTimeout10_Success()
        {
              Assert.Okay();
        }

        // NUnit's key/value metadata, all three ways it is written in real
        // suites: [Property] directly (string and int overloads, and a
        // repeated key), the built-in [Author] subclass, and a custom
        // subclass whose property name comes from its class name.
        [Test]
        [Property("Bug", "1234")]
        [Property("Bug", 5678)]
        [Property("Weight", 2.5)]
        [Author("anyunit", "dev@example.invalid")]
        [Severity("High")]
        public void TestProperties_Success()
        {
            Assert.Okay();
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

        [Test, Ignore("Ignoring...")]
        public void Test_Ignore()
        {
           Assert.Okay();
        }

        [Test]
        public void TestParams_Success([Range(1,5)]int i)
        {
            Assert.True(i < 10, "You shouldn't seee this message");
        }

        [Test]
        public void TestParams_Fail([Range(1, 5)]int i)
        {
            Assert.True(i > 10, String.Format("expected {0} to be greater than 10", i));
        }


        [TestCase(1, Expected._Fail)]
        [TestCase(2, Expected._Fail)]
        [TestCase(3, Expected._Fail)]
        [TestCase(4, Expected._Success)]
        [TestCase(5, Expected._Success)]
        public void TestParams_Partial(int i, Expected expected)
        {
            Assert.True(i > 3, String.Format("expected {0} to be greater than 3", i));
        }

        [Test]
        [TestCase(1, Expected._Success)]
        [TestCase(2, Expected._Success)]
        [TestCase(3, Expected._Fail)]
        [TestCase(4, Expected._Success)]
        [TestCase(5, Expected._Success)]
        public void TestParams2_Partial(int i, Expected expected)
        {
            Assert.False(i == 3, String.Format("expected {0} to be greater than 3", i));
        }

    }

    // The subclassing pattern NUnit's own docs show: the property name is
    // the class name with "Attribute" removed, so this is Severity=High.
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Class, AllowMultiple = false)]
    public class SeverityAttribute : PropertyAttribute
    {
        public SeverityAttribute(string level) : base((object)level) { }
    }
}
