//
//  Copyright 2013 AnyUnit Contributors
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

using System;
using System.Collections;
using System.Reflection;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// The MSTest `Assert` vocabulary, as extension methods on IAssert -
    /// the same pattern AnyUnit.Style.Xunit/AssertEx.cs uses for xUnit's.
    /// Extension methods are enough here (unlike StringAssert /
    /// CollectionAssert, see AssertionHelper.cs) because a ported call site
    /// already writes an explicit receiver: `Assert.AreEqual(...)`, where
    /// `Assert` is the inherited IAssert property.
    ///
    /// Every method routes failure through assert.Fail and success through
    /// assert.Okay, never through a bare `throw`. That is load-bearing, not
    /// style: AssertCount is what separates ResultKind.Success from
    /// ResultKind.NoError, so an assertion that threw without counting
    /// would let a passing test report as "asserted nothing".
    ///
    /// Note what is deliberately absent: `Fail`. IAssert already declares
    /// Fail(string, IEnumerable&lt;string&gt;) as a real instance method, so
    /// `Assert.Fail("...")` in ported code already binds to it - and an
    /// instance method always beats an extension method anyway. Adding one
    /// here would be dead code at best and an ambiguity at worst. Same for
    /// IsTrue/IsFalse's AnyUnit spellings True/False, which stay available
    /// side by side.
    /// </summary>
    public static class AssertEx
    {
        // MSTest's own failure text shape, kept close enough that a
        // developer reading a failure recognizes it. One helper so the
        // "<actual>" / "(null)" rendering is consistent across every
        // assertion in this file and its two siblings.
        internal static string Describe(object value)
        {
            if (value == null)
                return "(null)";
            var str = value as string;
            if (str != null)
                return "\"" + str + "\"";
            return value.ToString();
        }

        internal static string WithMessage(string detail, string message)
        {
            if (string.IsNullOrEmpty(message))
                return detail;
            return detail + " " + message;
        }

        /// <summary>Asserts two values are equal, by Object.Equals.</summary>
        public static void AreEqual<T>(this IAssert assert, T expected, T actual, string message = null)
        {
            if (Equals(expected, actual))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.AreEqual failed. Expected:<{0}>. Actual:<{1}>.",
                              Describe(expected), Describe(actual)), message));
        }

        /// <summary>
        /// Asserts two doubles are equal within `delta`.
        ///
        /// Kept as its own overload rather than folded into the generic
        /// one: with T inferred as double the generic method would compare
        /// by Object.Equals - exact bit equality - which is precisely the
        /// comparison a caller passing a delta is asking not to use.
        /// </summary>
        public static void AreEqual(this IAssert assert, double expected, double actual, double delta, string message = null)
        {
            if (Math.Abs(expected - actual) <= delta)
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.AreEqual failed. Expected a difference no greater than <{0}> between expected value <{1}> and actual value <{2}>.",
                              delta, expected, actual), message));
        }

        /// <summary>Asserts two strings are equal, optionally ignoring case.</summary>
        public static void AreEqual(this IAssert assert, string expected, string actual, bool ignoreCase, string message = null)
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (string.Equals(expected, actual, comparison))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.AreEqual failed. Expected:<{0}>. Actual:<{1}>.",
                              Describe(expected), Describe(actual)), message));
        }

        /// <summary>Asserts two values are not equal.</summary>
        public static void AreNotEqual<T>(this IAssert assert, T notExpected, T actual, string message = null)
        {
            if (!Equals(notExpected, actual))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.AreNotEqual failed. Expected any value except:<{0}>. Actual:<{1}>.",
                              Describe(notExpected), Describe(actual)), message));
        }

        /// <summary>Asserts two references point at the same object.</summary>
        public static void AreSame(this IAssert assert, object expected, object actual, string message = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage("Assert.AreSame failed.", message));
        }

        /// <summary>Asserts two references do not point at the same object.</summary>
        public static void AreNotSame(this IAssert assert, object notExpected, object actual, string message = null)
        {
            if (!ReferenceEquals(notExpected, actual))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage("Assert.AreNotSame failed.", message));
        }

        /// <summary>Asserts a condition is true.</summary>
        public static void IsTrue(this IAssert assert, bool condition, string message = null)
        {
            assert.True(condition, WithMessage("Assert.IsTrue failed.", message));
        }

        /// <summary>Asserts a condition is false.</summary>
        public static void IsFalse(this IAssert assert, bool condition, string message = null)
        {
            assert.False(condition, WithMessage("Assert.IsFalse failed.", message));
        }

        /// <summary>Asserts a value is null.</summary>
        public static void IsNull(this IAssert assert, object value, string message = null)
        {
            if (value == null)
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.IsNull failed. Actual:<{0}>.", Describe(value)), message));
        }

        /// <summary>Asserts a value is not null.</summary>
        public static void IsNotNull(this IAssert assert, object value, string message = null)
        {
            if (value != null)
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage("Assert.IsNotNull failed.", message));
        }

        /// <summary>
        /// Asserts a value is an instance of `expectedType` - or of a
        /// subclass of it, which is MSTest's own behavior here (note the
        /// contrast with [ExpectedException], whose default is an exact
        /// match).
        /// </summary>
        public static void IsInstanceOfType(this IAssert assert, object value, Type expectedType, string message = null)
        {
            if (value != null && expectedType != null
                && expectedType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.IsInstanceOfType failed. Expected type:<{0}>. Actual type:<{1}>.",
                              expectedType, value == null ? "(null)" : value.GetType().ToString()), message));
        }

        /// <summary>Asserts a value is not an instance of `wrongType`.</summary>
        public static void IsNotInstanceOfType(this IAssert assert, object value, Type wrongType, string message = null)
        {
            if (value == null || wrongType == null
                || !wrongType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo()))
            {
                assert.Okay();
                return;
            }
            assert.Fail(WithMessage(
                string.Format("Assert.IsNotInstanceOfType failed. Wrong type:<{0}>.", wrongType), message));
        }

        /// <summary>
        /// Asserts `action` throws exactly T (not a subclass - MSTest's own
        /// rule), and returns the exception so the caller can go on to
        /// assert about its message.
        /// </summary>
        public static T ThrowsException<T>(this IAssert assert, Action action, string message = null)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T caught) when (caught.GetType() == typeof(T))
            {
                assert.Okay();
                return caught;
            }
            catch (Exception other)
            {
                // Deliberately a Fail, not a rethrow: a wrong exception type
                // is a failed expectation about the code under test, not an
                // error in the test harness, so it must land as
                // ResultKind.Fail. Preserving `other` as the inner exception
                // keeps its stack trace in the log.
                assert.Fail(new AssertionException(
                    WithMessage(string.Format("Assert.ThrowsException failed. Expected:<{0}>. Actual:<{1}>.",
                                              typeof(T), other.GetType()), message),
                    other));
            }

            assert.Fail(WithMessage(
                string.Format("Assert.ThrowsException failed. No exception thrown. Expected:<{0}>.", typeof(T)),
                message));

            // Unreachable: assert.Fail always throws. Present only because
            // C# cannot see that through the IAssert interface.
            return null;
        }

        /// <summary>
        /// As ThrowsException&lt;T&gt;(Action), for an expression with a
        /// value - MSTest ships this overload so `Assert.ThrowsException&lt;T&gt;(() =&gt; list[5])`
        /// compiles without the caller adding a discard.
        /// </summary>
        public static T ThrowsException<T>(this IAssert assert, Func<object> func, string message = null)
            where T : Exception
        {
            return assert.ThrowsException<T>(() => { func(); }, message);
        }

        /// <summary>
        /// MSTest's "this test reached no conclusion" outcome. Maps onto
        /// AnyUnit.IgnoreException - i.e. ResultKind.Ignore - which is the
        /// exact equivalent: neither pass nor fail, and reported as skipped
        /// by every results format AnyUnit.Report writes.
        /// </summary>
        public static void Inconclusive(this IAssert assert, string message = null)
        {
            assert.Ignore(message ?? "Assert.Inconclusive failed.");
        }

        // Shared by CollectionAssertions and used nowhere else - lives here
        // so the null/element comparison rule is written down once.
        internal static bool SequencesEqual(ICollection expected, ICollection actual)
        {
            if (ReferenceEquals(expected, actual))
                return true;
            if (expected == null || actual == null)
                return false;
            if (expected.Count != actual.Count)
                return false;

            var left = expected.GetEnumerator();
            var right = actual.GetEnumerator();
            while (left.MoveNext() && right.MoveNext())
            {
                if (!Equals(left.Current, right.Current))
                    return false;
            }
            return true;
        }
    }
}
