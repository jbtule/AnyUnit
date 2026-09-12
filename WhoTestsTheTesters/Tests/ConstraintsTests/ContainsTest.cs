using System.Collections.Generic;
using AnyUnit;
using AnyUnit.Constraints;

namespace ConstraintsTests
{
    // ContainsConstraint (behind Does.Contain(string)) postpones deciding
    // between substring and collection-element matching until it sees the
    // actual value's runtime type - both branches, plus the collection
    // overload that skips that dispatch entirely (Does.Contain(object)),
    // covered here.
    [TestFixture]
    public class ContainsTest : AssertionHelper
    {
        [Test]
        public void StringActual_SubstringMatch_Success()
        {
            Assert.That("Test", Does.Contain("es"));
        }

        [Test]
        public void StringActual_SubstringMatch_Fail()
        {
            Assert.That("Test", Does.Contain("xyz"));
        }

        [Test]
        public void StringActual_IgnoreCase_Success()
        {
            Assert.That("Test", Does.Contain("ES").IgnoreCase);
        }

        [Test]
        public void CollectionActual_ElementMatch_Success()
        {
            Assert.That(new[] { 1, 2, 3 }, Does.Contain(2));
        }

        [Test]
        public void CollectionActual_ElementMatch_Fail()
        {
            Assert.That(new[] { 1, 2, 3 }, Does.Contain(4));
        }

        [Test]
        public void ObjectOverload_CollectionOfStrings_Success()
        {
            // Does.Contain(object) always goes straight to
            // CollectionContainsConstraint - no runtime-type dispatch, even
            // for a collection of strings.
            Assert.That(new List<string> { "a", "b" }, Does.Contain((object)"b"));
        }
    }
}
