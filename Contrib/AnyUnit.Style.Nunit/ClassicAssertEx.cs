// ****************************************************************
// Copyright 2007, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using AnyUnit.Constraints;
using AnyUnit.Constraints.Pieces;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// NUnit's *classic* assertion model — <c>Assert.AreEqual</c>, <c>Assert.IsTrue</c>,
    /// <c>Assert.Throws&lt;T&gt;</c> and the rest — as extension methods on
    /// <see cref="IAssert"/>, so that an existing suite's call sites compile unchanged.
    /// </summary>
    /// <remarks>
    /// Inside a fixture deriving from <see cref="AnyUnit.Run.AssertionHelper"/>, <c>Assert</c> is the
    /// injected instance property and <c>Assert.AreEqual(a, b)</c> reads exactly as it did against
    /// NUnit's static class. Elsewhere — a fixture with no base class, an F# module-level test —
    /// spell it <c>Assert.Current.AreEqual(a, b)</c>.
    ///
    /// Equality is NUnit's, through <c>Is.EqualTo</c> and so through
    /// <c>NUnitEqualityComparer</c>: numeric values of different types compare by value, so
    /// <c>AreEqual(1, 1L)</c> passes, as it does in NUnit and as <c>object.Equals</c> would not.
    ///
    /// NUnit 4 keeps this model as <c>NUnit.Framework.Legacy.ClassicAssert</c>; the constraint
    /// model lives alongside it here as <c>Assert.That</c> and <see cref="AssertionHelper.Expect"/>.
    /// </remarks>
    public static class ClassicAssertEx
    {
        internal static readonly IList<string> ExcludeFromStack =
            new List<string> { string.Format("at {0}.", typeof(ClassicAssertEx).FullName) };

        private static void Check(IAssert assert, object actual, IResolveConstraint expression, string message, object[] args)
        {
            var constraint = expression.Resolve();
            if (constraint.Matches(actual))
            {
                assert.Okay();
                return;
            }
            using (MessageWriter writer = new TextMessageWriter(message, args ?? new object[0]))
            {
                constraint.WriteMessageTo(writer);
                assert.Fail(writer.ToString(), ExcludeFromStack);
            }
        }

        // --- equality ------------------------------------------------------------------------

        /// <summary>Verifies that two values are equal, by NUnit's equality.</summary>
        public static void AreEqual(this IAssert assert, object expected, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.EqualTo(expected), message, args);

        /// <summary>Verifies that two values are not equal, by NUnit's equality.</summary>
        public static void AreNotEqual(this IAssert assert, object expected, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Not.EqualTo(expected), message, args);

        /// <summary>Verifies that two references are the same object.</summary>
        public static void AreSame(this IAssert assert, object expected, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.SameAs(expected), message, args);

        /// <summary>Verifies that two references are not the same object.</summary>
        public static void AreNotSame(this IAssert assert, object expected, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Not.SameAs(expected), message, args);

        // --- booleans and null ---------------------------------------------------------------

        /// <summary>Verifies that a condition is true.</summary>
        public static void IsTrue(this IAssert assert, bool condition, string message = null, params object[] args)
            => Check(assert, condition, Is.True, message, args);

        /// <summary>Verifies that a condition is true.</summary>
        public static void True(this IAssert assert, bool condition, string message, params object[] args)
            => Check(assert, condition, Is.True, message, args);

        /// <summary>Verifies that a condition is false.</summary>
        public static void IsFalse(this IAssert assert, bool condition, string message = null, params object[] args)
            => Check(assert, condition, Is.False, message, args);

        /// <summary>Verifies that a condition is false.</summary>
        public static void False(this IAssert assert, bool condition, string message, params object[] args)
            => Check(assert, condition, Is.False, message, args);

        /// <summary>Verifies that a value is null.</summary>
        public static void IsNull(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Null, message, args);

        /// <summary>Verifies that a value is null.</summary>
        public static void Null(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Null, message, args);

        /// <summary>Verifies that a value is not null.</summary>
        public static void IsNotNull(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Not.Null, message, args);

        /// <summary>Verifies that a value is not null.</summary>
        public static void NotNull(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Not.Null, message, args);

        // --- ordering ------------------------------------------------------------------------

        /// <summary>Verifies that the first value is greater than the second.</summary>
        public static void Greater(this IAssert assert, object arg1, object arg2, string message = null, params object[] args)
            => Check(assert, arg1, Is.GreaterThan(arg2), message, args);

        /// <summary>Verifies that the first value is greater than or equal to the second.</summary>
        public static void GreaterOrEqual(this IAssert assert, object arg1, object arg2, string message = null, params object[] args)
            => Check(assert, arg1, Is.GreaterThanOrEqualTo(arg2), message, args);

        /// <summary>Verifies that the first value is less than the second.</summary>
        public static void Less(this IAssert assert, object arg1, object arg2, string message = null, params object[] args)
            => Check(assert, arg1, Is.LessThan(arg2), message, args);

        /// <summary>Verifies that the first value is less than or equal to the second.</summary>
        public static void LessOrEqual(this IAssert assert, object arg1, object arg2, string message = null, params object[] args)
            => Check(assert, arg1, Is.LessThanOrEqualTo(arg2), message, args);

        /// <summary>Verifies that a value is zero.</summary>
        public static void Zero(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.EqualTo(0), message, args);

        /// <summary>Verifies that a value is not zero.</summary>
        public static void NotZero(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Not.EqualTo(0), message, args);

        // --- types and collections -----------------------------------------------------------

        /// <summary>Verifies that a value is an instance of the given type.</summary>
        public static void IsInstanceOf<T>(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.InstanceOf(typeof(T)), message, args);

        /// <summary>Verifies that a value is not an instance of the given type.</summary>
        public static void IsNotInstanceOf<T>(this IAssert assert, object actual, string message = null, params object[] args)
            => Check(assert, actual, Is.Not.InstanceOf(typeof(T)), message, args);

        /// <summary>Verifies that a collection or string is empty.</summary>
        public static void IsEmpty(this IAssert assert, IEnumerable collection, string message = null, params object[] args)
            => Check(assert, collection, Is.Empty, message, args);

        /// <summary>Verifies that a collection or string is not empty.</summary>
        public static void IsNotEmpty(this IAssert assert, IEnumerable collection, string message = null, params object[] args)
            => Check(assert, collection, Is.Not.Empty, message, args);

        /// <summary>Verifies that a collection contains the expected value.</summary>
        public static void Contains(this IAssert assert, object expected, IEnumerable actual, string message = null, params object[] args)
            => Check(assert, actual, Has.Member(expected), message, args);

        // --- exceptions ----------------------------------------------------------------------

        /// <summary>
        /// Verifies that the code throws exactly <typeparamref name="T"/>, and returns the
        /// exception so that the caller can assert on it.
        /// </summary>
        public static T Throws<T>(this IAssert assert, TestDelegate code, string message = null, params object[] args) where T : Exception
            => (T)assert.Throws(typeof(T), code, message, args);

        /// <summary>
        /// Verifies that the code throws exactly <paramref name="expectedType"/>, and returns the
        /// exception so that the caller can assert on it.
        /// </summary>
        public static Exception Throws(this IAssert assert, Type expectedType, TestDelegate code, string message = null, params object[] args)
        {
            var caught = Catch(code);
            Check(assert, caught, Is.InstanceOf(expectedType), message, args);
            return caught;
        }

        /// <summary>
        /// Verifies that the code throws <typeparamref name="T"/> or a type derived from it, and
        /// returns the exception.
        /// </summary>
        public static T Catch<T>(this IAssert assert, TestDelegate code, string message = null, params object[] args) where T : Exception
        {
            var caught = Catch(code);
            Check(assert, caught, Is.InstanceOf(typeof(T)), message, args);
            return (T)caught;
        }

        /// <summary>Verifies that the code throws something, and returns the exception.</summary>
        public static Exception Catch(this IAssert assert, TestDelegate code, string message = null, params object[] args)
        {
            var caught = Catch(code);
            Check(assert, caught, Is.Not.Null, message, args);
            return caught;
        }

        /// <summary>Verifies that the code does not throw.</summary>
        public static void DoesNotThrow(this IAssert assert, TestDelegate code, string message = null, params object[] args)
            => Check(assert, Catch(code), Is.Null, message, args);

        private static Exception Catch(TestDelegate code)
        {
            if (code == null)
                throw new ArgumentNullException("code");
            try
            {
                code();
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        // --- exact-arity overloads -----------------------------------------------------------
        //
        // A method group converts to a delegate only through an overload whose signature matches
        // exactly: an optional parameter or a `params` tail does not count. Real NUnit has these,
        // so `new Action<bool>(Assert.IsFalse)` and `new Action(Assert.Fail)` compile there.

        /// <summary>Verifies that a condition is true. Exact arity, for a method group.</summary>
        public static void IsTrue(this IAssert assert, bool condition)
            => Check(assert, condition, Is.True, null, null);

        /// <summary>Verifies that a condition is false. Exact arity, for a method group.</summary>
        public static void IsFalse(this IAssert assert, bool condition)
            => Check(assert, condition, Is.False, null, null);

        /// <summary>Verifies that two values are equal. Exact arity, for a method group.</summary>
        public static void AreEqual(this IAssert assert, object expected, object actual)
            => Check(assert, actual, Is.EqualTo(expected), null, null);

        /// <summary>Fails the test. Exact arity, for a method group.</summary>
        public static void Fail(this IAssert assert)
            => assert.Fail(null, ExcludeFromStack);

        // --- outcomes ------------------------------------------------------------------------

        /// <summary>Marks the test as passed without a further check.</summary>
        public static void Pass(this IAssert assert, string message = null, params object[] args)
            => assert.Okay();
    }
}
