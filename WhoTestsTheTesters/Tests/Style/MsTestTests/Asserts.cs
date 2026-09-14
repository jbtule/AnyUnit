using System.Collections.Generic;
using System.Text.RegularExpressions;
using AnyUnit.Style.MsTest;

namespace MsTestTests
{
    // The point of these two fixtures is the BARE simple names
    // `StringAssert` and `CollectionAssert`: in real MSTest those are static
    // classes, and a bare simple name is never resolved by extension lookup,
    // so they have to be declared instance properties on AssertionHelper for
    // a ported call site like this to compile unchanged. If that decision
    // ever gets revisited, these files stop building - which is the point.
    [TestClass]
    public class StringAsserts : AssertionHelper
    {
        [TestMethod]
        public void Contains_Success()
        {
            StringAssert.Contains("hello world", "lo wo");
        }

        [TestMethod]
        public void Contains_Fail()
        {
            StringAssert.Contains("hello world", "goodbye");
        }

        [TestMethod]
        public void DoesNotContain_Success()
        {
            StringAssert.DoesNotContain("hello world", "goodbye");
        }

        [TestMethod]
        public void StartsWith_Success()
        {
            StringAssert.StartsWith("hello world", "hello");
        }

        [TestMethod]
        public void EndsWith_Fail()
        {
            StringAssert.EndsWith("hello world", "hello");
        }

        [TestMethod]
        public void Matches_Success()
        {
            StringAssert.Matches("abc123", new Regex(@"^[a-z]+\d+$"));
        }

        [TestMethod]
        public void DoesNotMatch_Success()
        {
            StringAssert.DoesNotMatch("abc123", new Regex(@"^\d+$"));
        }
    }

    [TestClass]
    public class CollectionAsserts : AssertionHelper
    {
        [TestMethod]
        public void AreEqual_Success()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, new List<int> { 1, 2, 3 });
        }

        [TestMethod]
        public void AreEqual_Fail()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, new[] { 3, 2, 1 });
        }

        // Equivalence is multiset equality, not set equality - order is
        // ignored, multiplicity is not.
        [TestMethod]
        public void AreEquivalent_Success()
        {
            CollectionAssert.AreEquivalent(new[] { 1, 2, 2 }, new[] { 2, 1, 2 });
        }

        [TestMethod]
        public void AreEquivalentDifferentMultiplicity_Fail()
        {
            CollectionAssert.AreEquivalent(new[] { 1, 1, 2 }, new[] { 1, 2, 2 });
        }

        [TestMethod]
        public void Contains_Success()
        {
            CollectionAssert.Contains(new[] { "a", "b" }, "b");
        }

        [TestMethod]
        public void DoesNotContain_Success()
        {
            CollectionAssert.DoesNotContain(new[] { "a", "b" }, "c");
        }

        // A null element must be caught by the assertion, not blow it up
        // from the inside - which is why the multiset tally uses a
        // placeholder key rather than putting null into a Dictionary.
        [TestMethod]
        public void AllItemsAreNotNull_Fail()
        {
            CollectionAssert.AllItemsAreNotNull(new[] { "a", null, "b" });
        }

        [TestMethod]
        public void AllItemsAreNotNullEquivalent_Success()
        {
            CollectionAssert.AreEquivalent(new[] { "a", null }, new[] { null, "a" });
        }

        [TestMethod]
        public void AllItemsAreUnique_Fail()
        {
            CollectionAssert.AllItemsAreUnique(new[] { 1, 2, 1 });
        }

        [TestMethod]
        public void IsSubsetOf_Success()
        {
            CollectionAssert.IsSubsetOf(new[] { 1, 2 }, new[] { 3, 2, 1 });
        }
    }
}
