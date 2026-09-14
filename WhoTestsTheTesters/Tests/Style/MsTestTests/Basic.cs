using System;
using AnyUnit.Style.MsTest;

namespace MsTestTests
{
    [TestClass]
    public class Basic : AssertionHelper
    {
        [TestMethod]
        public void IsTrue_Success()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void IsTrue_Fail()
        {
            Assert.IsTrue(false);
        }

        [TestMethod]
        public void IsFalse_Success()
        {
            Assert.IsFalse(false);
        }

        [TestMethod]
        public void AreEqual_Success()
        {
            Assert.AreEqual(2, 1 + 1);
        }

        [TestMethod]
        public void AreEqual_Fail()
        {
            Assert.AreEqual(3, 1 + 1, "one plus one is not three");
        }

        [TestMethod]
        public void AreEqualWithDelta_Success()
        {
            Assert.AreEqual(1.0, 1.05, 0.1);
        }

        [TestMethod]
        public void AreNotEqual_Success()
        {
            Assert.AreNotEqual("a", "b");
        }

        [TestMethod]
        public void IsNull_Success()
        {
            Assert.IsNull(null);
        }

        [TestMethod]
        public void IsNotNull_Fail()
        {
            Assert.IsNotNull(null);
        }

        [TestMethod]
        public void IsInstanceOfType_Success()
        {
            Assert.IsInstanceOfType("a string", typeof(string));
        }

        [TestMethod]
        public void IsInstanceOfType_Fail()
        {
            Assert.IsInstanceOfType("a string", typeof(int));
        }

        [TestMethod]
        public void ThrowsException_Success()
        {
            var ex = Assert.ThrowsException<InvalidOperationException>(() => { throw new InvalidOperationException("boom"); });
            Assert.AreEqual("boom", ex.Message);
        }

        // The wrong exception type is a failed expectation about the code
        // under test, not a broken harness - so Fail, not Error. See
        // AssertEx.ThrowsException.
        [TestMethod]
        public void ThrowsExceptionWrongType_Fail()
        {
            Assert.ThrowsException<InvalidOperationException>(() => { throw new ArgumentException("wrong"); });
        }

        [TestMethod]
        public void ThrowsExceptionNothingThrown_Fail()
        {
            Assert.ThrowsException<InvalidOperationException>(() => { });
        }

        // Assert.Inconclusive maps onto AnyUnit.IgnoreException, so the
        // result is Ignore - neither pass nor fail.
        [TestMethod]
        public void Inconclusive_Ignore()
        {
            Assert.Inconclusive("nothing to conclude");
        }

        // No assertions and no exception: AnyUnit's own ResultKind.NoError,
        // which is the distinction the instance-Assert design exists to
        // preserve. MSTest itself would just call this a pass.
        [TestMethod]
        public void Nothing_NoError()
        {
        }

        [TestMethod]
        public void Throw_Error()
        {
            throw new NotSupportedException("This should be an error.");
        }

        [TestMethod, Ignore("Ignoring...")]
        public void Ignored_Ignore()
        {
            Assert.IsTrue(true);
        }

        // Exercises GetTimeout's non-default path without actually spending
        // the timeout: a test that finishes well inside its budget. A
        // deliberately-expiring timeout test is left to BasicTests/
        // NunitTests, which already cover the core's enforcement.
        [TestMethod, Timeout(5000)]
        public void WithinTimeout_Success()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        [Description("Carries every metadata attribute at once")]
        [TestCategory("Fast", "Unit")]
        [Owner("anyunit")]
        [Priority(1)]
        [TestProperty("Layer", "core")]
        public void Metadata_Success()
        {
            Assert.IsNotNull(TestContext);
            Assert.AreEqual("Metadata_Success", TestContext.TestName);
            Assert.AreEqual("MsTestTests.Basic", TestContext.FullyQualifiedTestClassName);
            TestContext.WriteLine("a literal brace { survives WriteLine");
        }
    }

    [TestClass]
    public class ExpectedExceptions : AssertionHelper
    {
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void Thrown_Success()
        {
            throw new InvalidOperationException("expected");
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void NotThrown_Fail()
        {
            Assert.IsTrue(true);
        }

        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void WrongType_Fail()
        {
            throw new ArgumentException("not what was asked for");
        }

        // MSTest's default is an EXACT type match, so a subclass does not
        // satisfy the expectation unless AllowDerivedTypes says so.
        [TestMethod, ExpectedException(typeof(Exception))]
        public void DerivedNotAllowed_Fail()
        {
            throw new InvalidOperationException("a subclass of Exception");
        }

        [TestMethod, ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void DerivedAllowed_Success()
        {
            throw new InvalidOperationException("a subclass of Exception");
        }
    }

    // A class-level [Ignore] skips every test in the class - the
    // IgnoreException is thrown from fixture construction, and
    // TestCycleExceptions.GetResult treats an ignore from any cycle as a skip.
    [TestClass, Ignore("whole class is out")]
    public class WholeClass : AssertionHelper
    {
        [TestMethod]
        public void First_Ignore()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Second_Ignore()
        {
            Assert.IsTrue(false);
        }
    }
}
