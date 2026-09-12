using System;
using AnyUnit;
using AnyUnit.Run;

namespace BootstrapTests
{
    // Same convention as WhoTestsTheTesters/Tests/BasicTests/Basic.cs's
    // own comment: one test per ResultKind, named per
    // WhoTestsTheTesters/ConventionTestProcessor's own convention
    // (_Success/_Fail/_Error/_Ignore/_NoError substrings, matched against
    // Result.Test.Name).
    [TestFixture]
    public class Basic : AssertionHelper
    {
        [Test]
        public void TestTrue_Success()
        {
            Assert.True(true);
        }

        [Test]
        public void TestFalse_Fail()
        {
            Assert.False(true, "Expected False");
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
    }
}
