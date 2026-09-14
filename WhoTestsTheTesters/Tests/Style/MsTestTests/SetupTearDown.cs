using System;
using AnyUnit.Style.MsTest;

namespace MsTestTests
{
    [TestClass]
    public class InitializeAndCleanup : AssertionHelper
    {
        private bool _initialized;

        [TestInitialize]
        public void Initialize()
        {
            _initialized = true;
        }

        [TestMethod]
        public void RanInitialize_Success()
        {
            Assert.IsTrue(_initialized);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _initialized = false;
        }
    }

    // A throw from [TestInitialize] is tagged TestCycle.Setup, which
    // TestCycleExceptions.GetResult always calls an Error - never a Fail,
    // even when the exception is an assertion failure.
    [TestClass]
    public class InitializeThrows : AssertionHelper
    {
        [TestInitialize]
        public void Initialize()
        {
            throw new NotImplementedException("Initialize error");
        }

        [TestMethod]
        public void Test_Error()
        {
            Assert.IsTrue(true);
        }
    }

    [TestClass]
    public class CleanupThrows : AssertionHelper
    {
        [TestMethod]
        public void Test_Error()
        {
            Assert.IsTrue(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            throw new NotImplementedException("Cleanup error");
        }
    }

    // Cleanup must run even when the test body failed, and its own throw
    // must not be swallowed by the body's.
    [TestClass]
    public class CleanupThrowsAfterFailure : AssertionHelper
    {
        [TestMethod]
        public void Test_Error()
        {
            Assert.IsTrue(false);
        }

        [TestCleanup]
        public void Cleanup()
        {
            throw new NotImplementedException("Cleanup error");
        }
    }

    // What a ported [TestCleanup] actually reads TestContext for.
    [TestClass]
    public class CleanupReadsOutcome : AssertionHelper
    {
        [TestMethod]
        public void Passing_Success()
        {
            Assert.IsTrue(true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (TestContext == null)
                throw new InvalidOperationException("TestContext was never injected");
            if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
                throw new InvalidOperationException("expected Passed, got " + TestContext.CurrentTestOutcome);
        }
    }

    [TestClass]
    public class ClassInitializeAndCleanup : AssertionHelper
    {
        private static string _seenClassName;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _seenClassName = context.FullyQualifiedTestClassName;
        }

        [TestMethod]
        public void ClassInitializeRan_Success()
        {
            Assert.AreEqual("MsTestTests.ClassInitializeAndCleanup", _seenClassName);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _seenClassName = null;
        }
    }

    // MSTest's own convention: the class declares its own TestContext
    // property and the framework property-injects it. That declaration
    // HIDES the one inherited from AssertionHelper, which is exactly why
    // TestContext.Inject reflects on the runtime type instead of assigning
    // through a base-typed reference.
    [TestClass]
    public class DeclaresOwnTestContext : AssertionHelper
    {
        public new TestContext TestContext { get; set; }

        [TestMethod]
        public void OwnPropertyInjected_Success()
        {
            Assert.IsNotNull(TestContext);
            Assert.AreEqual("OwnPropertyInjected_Success", TestContext.TestName);
        }
    }
}
