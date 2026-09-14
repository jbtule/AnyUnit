//
//  Copyright 2026 AnyUnit Contributors
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// Instance stand-in for MSTest's static CollectionAssert class,
    /// reached as the bare name `CollectionAssert` through
    /// AssertionHelper's declared property of that name. See
    /// AssertionHelper.cs for the resolution rule that forces a declared
    /// property here, and StringAssertions.cs for why the type name and the
    /// property name differ.
    ///
    /// Signatures take ICollection rather than IEnumerable to match real
    /// MSTest's, so ported call sites that pass an array or a List&lt;T&gt;
    /// bind identically. A ported call passing a bare IEnumerable&lt;T&gt;
    /// (a LINQ query result) did not compile against real MSTest either and
    /// still needs a .ToList().
    /// </summary>
    public class CollectionAssertions
    {
        private readonly IAssert _assert;

        public CollectionAssertions(IAssert assert)
        {
            _assert = assert;
        }

        /// <summary>Asserts two collections have the same elements in the same order.</summary>
        public void AreEqual(ICollection expected, ICollection actual, string message = null)
        {
            if (AssertEx.SequencesEqual(expected, actual))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.AreEqual failed. Expected:<{0}>. Actual:<{1}>.",
                              Render(expected), Render(actual)), message));
        }

        /// <summary>Asserts two collections differ in element or order.</summary>
        public void AreNotEqual(ICollection notExpected, ICollection actual, string message = null)
        {
            if (!AssertEx.SequencesEqual(notExpected, actual))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.AreNotEqual failed. Expected any value except:<{0}>.",
                              Render(notExpected)), message));
        }

        /// <summary>
        /// Asserts two collections hold the same elements, in any order and
        /// with the same multiplicity - "equivalent" in MSTest's sense is
        /// multiset equality, not set equality, so {1,1,2} and {1,2,2} are
        /// NOT equivalent even though they contain the same distinct values.
        /// </summary>
        public void AreEquivalent(ICollection expected, ICollection actual, string message = null)
        {
            if (Equivalent(expected, actual))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.AreEquivalent failed. Expected:<{0}>. Actual:<{1}>.",
                              Render(expected), Render(actual)), message));
        }

        /// <summary>Asserts two collections are not multiset-equal.</summary>
        public void AreNotEquivalent(ICollection notExpected, ICollection actual, string message = null)
        {
            if (!Equivalent(notExpected, actual))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.AreNotEquivalent failed. Expected any value except:<{0}>.",
                              Render(notExpected)), message));
        }

        /// <summary>Asserts a collection contains a given element.</summary>
        public void Contains(ICollection collection, object element, string message = null)
        {
            if (collection != null && collection.Cast<object>().Any(it => Equals(it, element)))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.Contains failed. Collection:<{0}>. Element:<{1}>.",
                              Render(collection), AssertEx.Describe(element)), message));
        }

        /// <summary>Asserts a collection does not contain a given element.</summary>
        public void DoesNotContain(ICollection collection, object element, string message = null)
        {
            if (collection == null || !collection.Cast<object>().Any(it => Equals(it, element)))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.DoesNotContain failed. Collection:<{0}>. Element:<{1}>.",
                              Render(collection), AssertEx.Describe(element)), message));
        }

        /// <summary>Asserts no element of a collection is null.</summary>
        public void AllItemsAreNotNull(ICollection collection, string message = null)
        {
            if (collection != null && collection.Cast<object>().All(it => it != null))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.AllItemsAreNotNull failed. Collection:<{0}>.",
                              Render(collection)), message));
        }

        /// <summary>Asserts no element of a collection is duplicated.</summary>
        public void AllItemsAreUnique(ICollection collection, string message = null)
        {
            if (collection != null)
            {
                var items = collection.Cast<object>().ToList();
                if (items.Distinct().Count() == items.Count)
                {
                    _assert.Okay();
                    return;
                }
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.AllItemsAreUnique failed. Collection:<{0}>.",
                              Render(collection)), message));
        }

        /// <summary>Asserts every element of `subset` also appears in `superset`.</summary>
        public void IsSubsetOf(ICollection subset, ICollection superset, string message = null)
        {
            if (subset != null && superset != null)
            {
                var outer = superset.Cast<object>().ToList();
                if (subset.Cast<object>().All(it => outer.Any(o => Equals(o, it))))
                {
                    _assert.Okay();
                    return;
                }
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("CollectionAssert.IsSubsetOf failed. Subset:<{0}>. Superset:<{1}>.",
                              Render(subset), Render(superset)), message));
        }

        // Multiset equality, done by grouping rather than by sorting:
        // sorting would demand the elements be IComparable, which MSTest
        // never requires and a collection of arbitrary objects rarely is.
        private static bool Equivalent(ICollection expected, ICollection actual)
        {
            if (ReferenceEquals(expected, actual))
                return true;
            if (expected == null || actual == null)
                return false;
            if (expected.Count != actual.Count)
                return false;

            var expectedCounts = Tally(expected);
            var actualCounts = Tally(actual);

            if (expectedCounts.Count != actualCounts.Count)
                return false;

            foreach (var pair in expectedCounts)
            {
                int count;
                if (!actualCounts.TryGetValue(pair.Key, out count) || count != pair.Value)
                    return false;
            }
            return true;
        }

        // A plain Dictionary<object,int> would throw on a null element, so
        // nulls are tallied separately under their own counter. A null in a
        // collection under test is exactly the kind of thing an assertion
        // is there to catch, so it must not make the assertion itself blow
        // up with an unrelated NullReferenceException.
        private static Dictionary<object, int> Tally(ICollection collection)
        {
            var counts = new Dictionary<object, int>();
            var nullKey = NullPlaceholder;
            foreach (var item in collection)
            {
                var key = item ?? nullKey;
                int existing;
                counts.TryGetValue(key, out existing);
                counts[key] = existing + 1;
            }
            return counts;
        }

        private static readonly object NullPlaceholder = new object();

        private static string Render(ICollection collection)
        {
            if (collection == null)
                return "(null)";
            return "[" + string.Join(", ", collection.Cast<object>().Select(AssertEx.Describe).ToArray()) + "]";
        }
    }
}
