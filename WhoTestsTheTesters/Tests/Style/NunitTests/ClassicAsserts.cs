using System;
using System.Collections.Generic;
using AnyUnit.Style.Nunit;
using AnyUnit.Constraints;

namespace NunitTests
{
    /// <summary>
    /// NUnit's classic model, written the way an existing NUnit suite writes it: a fixture
    /// deriving from <c>AssertionHelper</c>, and <c>Assert.AreEqual(...)</c> with no <c>this</c>.
    /// </summary>
    [TestFixture]
    public class ClassicAsserts_Success : AssertionHelper
    {
        [Test]
        public void Equality_Success()
        {
            Assert.AreEqual(2, 1 + 1);
            Assert.AreEqual("ab", "a" + "b", "concatenation");
            Assert.AreNotEqual(3, 1 + 1);
        }

        [Test]
        public void Equality_widens_numerics_as_NUnit_does_Success()
        {
            Assert.AreEqual(1, 1L);
            Assert.AreEqual(1.0, 1);
            Assert.AreEqual(1m, 1);
        }

        [Test]
        public void Same_and_not_same_Success()
        {
            var o = new object();
            Assert.AreSame(o, o);
            Assert.AreNotSame(o, new object());
        }

        [Test]
        public void Booleans_and_null_Success()
        {
            Assert.IsTrue(true);
            Assert.IsFalse(false);
            Assert.IsNull(null);
            Assert.IsNotNull("x");
            Assert.NotNull("x");
        }

        [Test]
        public void Ordering_Success()
        {
            Assert.Less(1, 2);
            Assert.LessOrEqual(2, 2);
            Assert.Greater(2, 1);
            Assert.GreaterOrEqual(2, 2);
            Assert.Zero(0);
            Assert.NotZero(1);
        }

        [Test]
        public void Types_and_collections_Success()
        {
            Assert.IsInstanceOf<string>("x");
            Assert.IsNotInstanceOf<string>(1);
            Assert.IsEmpty(new int[0]);
            Assert.IsNotEmpty(new[] { 1 });
            Assert.Contains(2, new[] { 1, 2, 3 });
        }

        [Test]
        public void Throws_returns_the_exception_Success()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => throw new InvalidOperationException("boom"));
            Assert.AreEqual("boom", ex.Message);

            var caught = Assert.Catch<Exception>(() => throw new InvalidOperationException("also boom"));
            Assert.AreEqual("also boom", caught.Message);

            Assert.DoesNotThrow(() => { });
        }

        [Test]
        public void Through_Assert_Current_without_a_this_Success()
        {
            AnyUnit.Run.Assert.Current.AreEqual(2, 1 + 1);
        }
    }

    /// <summary>The same assertions failing, so the failure path is exercised too.</summary>
    [TestFixture]
    public class ClassicAsserts_FailurePaths : AssertionHelper
    {
        private void Fails(TestDelegate code)
        {
            try
            {
                code();
            }
            catch (AnyUnit.AssertionException)
            {
                Assert.Okay();
                return;
            }
            Assert.Fail("expected the assertion to fail");
        }

        [Test]
        public void Each_kind_reports_a_failure_Success()
        {
            Fails(() => Assert.AreEqual(1, 2));
            Fails(() => Assert.AreNotEqual(1, 1));
            Fails(() => Assert.IsTrue(false));
            Fails(() => Assert.IsFalse(true));
            Fails(() => Assert.IsNull("x"));
            Fails(() => Assert.IsNotNull(null));
            Fails(() => Assert.Less(2, 1));
            Fails(() => Assert.Greater(1, 2));
            Fails(() => Assert.IsInstanceOf<string>(1));
            Fails(() => Assert.Contains(4, new[] { 1, 2, 3 }));
            Fails(() => Assert.Throws<InvalidOperationException>(() => { }));
            Fails(() => Assert.DoesNotThrow(() => throw new Exception("thrown")));
        }

        [Test]
        public void A_failure_message_names_expected_and_actual_Success()
        {
            try
            {
                Assert.AreEqual(1, 2, "counting");
            }
            catch (AnyUnit.AssertionException ex)
            {
                Assert.IsTrue(ex.Message.Contains("counting"), "carries the user message");
                Assert.IsTrue(ex.Message.Contains("1"), "names the expected value");
                Assert.IsTrue(ex.Message.Contains("2"), "names the actual value");
                return;
            }
            Assert.Fail("expected the assertion to fail");
        }
    }
}
