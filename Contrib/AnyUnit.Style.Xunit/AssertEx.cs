﻿/*
 * Copyright 2013 Outercurve Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using AnyUnit.Style.Xunit.Exceptions;
using AnyUnit.Style.Xunit.Util;
using System.Reflection;
using AnyUnit.Util;

namespace AnyUnit.Style.Xunit
{



    public static class AssertEx
    {

        /// <summary>
        /// Verifies that a collection contains a given object.
        /// </summary>
        /// <typeparam name="T">The type of the object to be verified</typeparam>
        /// <param name="expected">The object expected to be in the collection</param>
        /// <param name="collection">The collection to be inspected</param>
        /// <exception cref="ContainsException">Thrown when the object is not present in the collection</exception>
        public static void Contains<T>(this IAssert assert,T expected, IEnumerable<T> collection)
        {
            assert.Contains(expected, collection, GetEqualityComparer<T>());
        }

        /// <summary>
        /// Verifies that a collection contains a given object, using an equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the object to be verified</typeparam>
        /// <param name="expected">The object expected to be in the collection</param>
        /// <param name="collection">The collection to be inspected</param>
        /// <param name="comparer">The comparer used to equate objects in the collection with the expected object</param>
        /// <exception cref="ContainsException">Thrown when the object is not present in the collection</exception>
        public static void Contains<T>(this IAssert assert, T expected, IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            Guard.ArgumentNotNull("comparer", comparer);

            if (collection != null)
                foreach (T item in collection)
                    if (comparer.Equals(expected, item))
                    {
                        assert.Okay();
                        return;
                    }

            assert.Fail(new ContainsException(expected));
        }

        /// <summary>
        /// Verifies that a string contains a given sub-string, using the current culture.
        /// </summary>
        /// <param name="expectedSubstring">The sub-string expected to be in the string</param>
        /// <param name="actualString">The string to be inspected</param>
        /// <exception cref="ContainsException">Thrown when the sub-string is not present inside the string</exception>
        public static void Contains(this IAssert assert, string expectedSubstring, string actualString)
        {
            assert.Contains(expectedSubstring, actualString, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Verifies that a string contains a given sub-string, using the given comparison type.
        /// </summary>
        /// <param name="expectedSubstring">The sub-string expected to be in the string</param>
        /// <param name="actualString">The string to be inspected</param>
        /// <param name="comparisonType">The type of string comparison to perform</param>
        /// <exception cref="ContainsException">Thrown when the sub-string is not present inside the string</exception>
        public static void Contains(this IAssert assert, string expectedSubstring, string actualString, StringComparison comparisonType)
        {
            if (actualString == null || actualString.IndexOf(expectedSubstring, comparisonType) < 0)
                assert.Fail(new ContainsException(expectedSubstring, actualString)); 
            assert.Okay();
        }

        /// <summary>
        /// Verifies that a collection does not contain a given object.
        /// </summary>
        /// <typeparam name="T">The type of the object to be compared</typeparam>
        /// <param name="expected">The object that is expected not to be in the collection</param>
        /// <param name="collection">The collection to be inspected</param>
        /// <exception cref="DoesNotContainException">Thrown when the object is present inside the container</exception>
        public static void DoesNotContain<T>(this IAssert assert, T expected, IEnumerable<T> collection)
        {
            assert.DoesNotContain(expected, collection, GetEqualityComparer<T>());
        }

        /// <summary>
        /// Verifies that a collection does not contain a given object, using an equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the object to be compared</typeparam>
        /// <param name="expected">The object that is expected not to be in the collection</param>
        /// <param name="collection">The collection to be inspected</param>
        /// <param name="comparer">The comparer used to equate objects in the collection with the expected object</param>
        /// <exception cref="DoesNotContainException">Thrown when the object is present inside the container</exception>
        public static void DoesNotContain<T>(this IAssert assert, T expected, IEnumerable<T> collection, IEqualityComparer<T> comparer)
        {
            Guard.ArgumentNotNull("comparer", comparer);

            if (collection != null)
                foreach (T item in collection)
                    if (comparer.Equals(expected, item))
                        assert.Fail(new DoesNotContainException(expected));

            assert.Okay();
        }

        /// <summary>
        /// Verifies that a string does not contain a given sub-string, using the current culture.
        /// </summary>
        /// <param name="expectedSubstring">The sub-string which is expected not to be in the string</param>
        /// <param name="actualString">The string to be inspected</param>
        /// <exception cref="DoesNotContainException">Thrown when the sub-string is present inside the string</exception>
        public static void DoesNotContain(this IAssert assert, string expectedSubstring, string actualString)
        {
            assert.DoesNotContain(expectedSubstring, actualString, StringComparison.CurrentCulture);
        }

        /// <summary>
        /// Verifies that a string does not contain a given sub-string, using the current culture.
        /// </summary>
        /// <param name="expectedSubstring">The sub-string which is expected not to be in the string</param>
        /// <param name="actualString">The string to be inspected</param>
        /// <param name="comparisonType">The type of string comparison to perform</param>
        /// <exception cref="DoesNotContainException">Thrown when the sub-string is present inside the given string</exception>
        public static void DoesNotContain(this IAssert assert, string expectedSubstring, string actualString, StringComparison comparisonType)
        {
            if (actualString != null && actualString.IndexOf(expectedSubstring, comparisonType) >= 0)
                assert.Fail(new DoesNotContainException(expectedSubstring));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that a block of code does not throw any exceptions.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        public static void DoesNotThrow(this IAssert assert, Action testCode)
        {
            Exception ex = Record.Exception(testCode);

            if (ex != null)
                assert.Fail(new DoesNotThrowException(ex)); 
            assert.Okay();
        }

        /// <summary>
        /// Verifies that a block of code does not throw any exceptions.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        public static void DoesNotThrow(this IAssert assert, Func<object> testCode)
        {
            Exception ex = Record.Exception(testCode);

            if (ex != null)
                assert.Fail(new DoesNotThrowException(ex));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that a collection is empty.
        /// </summary>
        /// <param name="collection">The collection to be inspected</param>
        /// <exception cref="ArgumentNullException">Thrown when the collection is null</exception>
        /// <exception cref="EmptyException">Thrown when the collection is not empty</exception>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "object", Justification = "No can do")]
        public static void Empty(this IAssert assert, IEnumerable collection)
        {
            Guard.ArgumentNotNull("collection", collection);

#pragma warning disable 168
            foreach (object @object in collection)
                assert.Fail(new EmptyException());
#pragma warning restore 168
            assert.Okay();
        }

        /// <summary>
        /// Verifies that two objects are equal, using a default comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <exception cref="EqualException">Thrown when the objects are not equal</exception>
        public static void Equal<T>(this IAssert assert, T expected, T actual)
        {
            assert.Equal(expected, actual, GetEqualityComparer<T>());
        }

        /// <summary>
        /// Verifies that two objects are equal, using a custom equatable comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="comparer">The comparer used to compare the two objects</param>
        /// <exception cref="EqualException">Thrown when the objects are not equal</exception>
        public static void Equal<T>(this IAssert assert, T expected, T actual, IEqualityComparer<T> comparer)
        {
            Guard.ArgumentNotNull("comparer", comparer);

            if (!comparer.Equals(expected, actual))
                assert.Fail(new EqualException(expected, actual));

            assert.Okay();
        }

        /// <summary>
        /// Verifies that two <see cref="double"/> values are equal, within the number of decimal
        /// places given by <paramref name="precision"/>.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="precision">The number of decimal places (valid values: 0-15)</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void Equal(this IAssert assert, double expected, double actual, int precision)
        {
            var expectedRounded = Math.Round(expected, precision);
            var actualRounded = Math.Round(actual, precision);

            if (!GetEqualityComparer<double>().Equals(expectedRounded, actualRounded))
                assert.Fail(new EqualException(
                    String.Format(CultureInfo.CurrentCulture, "{0} (rounded from {1})", expectedRounded, expected),
                    String.Format(CultureInfo.CurrentCulture, "{0} (rounded from {1})", actualRounded, actual)
                ));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that two <see cref="decimal"/> values are equal, within the number of decimal
        /// places given by <paramref name="precision"/>.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="precision">The number of decimal places (valid values: 0-15)</param>
        /// <exception cref="EqualException">Thrown when the values are not equal</exception>
        public static void Equal(this IAssert assert, decimal expected, decimal actual, int precision)
        {
            var expectedRounded = Math.Round(expected, precision);
            var actualRounded = Math.Round(actual, precision);

            if (!GetEqualityComparer<decimal>().Equals(expectedRounded, actualRounded))
                assert.Fail(new EqualException(
                    String.Format(CultureInfo.CurrentCulture, "{0} (rounded from {1})", expectedRounded, expected),
                    String.Format(CultureInfo.CurrentCulture, "{0} (rounded from {1})", actualRounded, actual)
                ));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that two sequences are equivalent, using a default comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <exception cref="EqualException">Thrown when the objects are not equal</exception>
        public static void Equal<T>(this IAssert assert, IEnumerable<T> expected, IEnumerable<T> actual)
        {
            assert.Equal<IEnumerable<T>>(expected, actual, GetEqualityComparer<IEnumerable<T>>(true));
        }

        /// <summary>
        /// Verifies that two sequences are equivalent, using a custom equatable comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected value</param>
        /// <param name="actual">The value to be compared against</param>
        /// <param name="comparer">The comparer used to compare the two objects</param>
        /// <exception cref="EqualException">Thrown when the objects are not equal</exception>
        public static void Equal<T>(this IAssert assert, IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        {
            assert.Equal<IEnumerable<T>>(expected, actual, GetEqualityComparer<IEnumerable<T>>(true, new AssertEqualityComparerAdapter<T>(comparer)));
        }

        /// <summary>
        /// Verifies that two strings are equal.
        /// </summary>
        /// <param name="expected">The expected string value.</param>
        /// <param name="actual">The actual string value.</param>
        /// <exception cref="EqualException">Thrown when the strings are not equivalent.</exception>
        public static void Equal(this IAssert assert, string expected, string actual)
        {
            assert.Equal(expected, actual, ignoreCase: false, ignoreLineEndingDifferences: false, ignoreWhiteSpaceDifferences: false);
        }

        /// <summary>
        /// Verifies that two strings are equivalent.
        /// </summary>
        /// <param name="expected">The expected string value.</param>
        /// <param name="actual">The actual string value.</param>
        /// <param name="ignoreCase">If set to <c>true</c>, ignores cases differences. The invariant culture is used.</param>
        /// <param name="ignoreLineEndingDifferences">If set to <c>true</c>, treats \r\n, \r, and \n as equivalent.</param>
        /// <param name="ignoreWhiteSpaceDifferences">If set to <c>true</c>, treats spaces and tabs (in any non-zero quantity) as equivalent.</param>
        /// <exception cref="EqualException">Thrown when the strings are not equivalent.</exception>
        public static void Equal(this IAssert assert, string expected, string actual, bool ignoreCase = false, bool ignoreLineEndingDifferences = false, bool ignoreWhiteSpaceDifferences = false)
        {
            // Start out assuming the one of the values is null
            int expectedIndex = -1;
            int actualIndex = -1;
            int expectedLength = 0;
            int actualLength = 0;

            if (expected == null)
            {
                if (actual == null)
                    return;
            }
            else if (actual != null)
            {
                // Walk the string, keeping separate indices since we can skip variable amounts of
                // data based on ignoreLineEndingDifferences and ignoreWhiteSpaceDifferences.
                expectedIndex = 0;
                actualIndex = 0;
                expectedLength = expected.Length;
                actualLength = actual.Length;

                while (expectedIndex < expectedLength && actualIndex < actualLength)
                {
                    char expectedChar = expected[expectedIndex];
                    char actualChar = actual[actualIndex];

                    if (ignoreLineEndingDifferences && IsLineEnding(expectedChar) && IsLineEnding(actualChar))
                    {
                        expectedIndex = SkipLineEnding(expected, expectedIndex);
                        actualIndex = SkipLineEnding(actual, actualIndex);
                    }
                    else if (ignoreWhiteSpaceDifferences && IsWhiteSpace(expectedChar) && IsWhiteSpace(actualChar))
                    {
                        expectedIndex = SkipWhitespace(expected, expectedIndex);
                        actualIndex = SkipWhitespace(actual, actualIndex);
                    }
                    else
                    {
                        if (ignoreCase)
                        {
                            expectedChar = Char.ToUpperInvariant(expectedChar);
                            actualChar = Char.ToUpperInvariant(actualChar);
                        }

                        if (expectedChar != actualChar)
                        {
                            break;
                        }

                        expectedIndex++;
                        actualIndex++;
                    }
                }
            }

            if (expectedIndex < expectedLength || actualIndex < actualLength)
            {
                assert.Fail(new EqualException(expected, actual, expectedIndex, actualIndex));
            }

            assert.Okay();
        }


        static IComparer<T> GetComparer<T>() where T : IComparable
        {
            return new AssertComparer<T>();
        }

        static IEqualityComparer<T> GetEqualityComparer<T>(bool skipTypeCheck = false, IEqualityComparer innerComparer = null)
        {
            return new AssertEqualityComparer<T>(skipTypeCheck, innerComparer);
        }

        /// <summary>
        /// Verifies that a value is within a given range.
        /// </summary>
        /// <typeparam name="T">The type of the value to be compared</typeparam>
        /// <param name="actual">The actual value to be evaluated</param>
        /// <param name="low">The (inclusive) low value of the range</param>
        /// <param name="high">The (inclusive) high value of the range</param>
        /// <exception cref="InRangeException">Thrown when the value is not in the given range</exception>
        public static void InRange<T>(this IAssert assert, T actual, T low, T high) where T : IComparable
        {
            assert.InRange(actual, low, high, GetComparer<T>());
        }

        /// <summary>
        /// Verifies that a value is within a given range, using a comparer.
        /// </summary>
        /// <typeparam name="T">The type of the value to be compared</typeparam>
        /// <param name="actual">The actual value to be evaluated</param>
        /// <param name="low">The (inclusive) low value of the range</param>
        /// <param name="high">The (inclusive) high value of the range</param>
        /// <param name="comparer">The comparer used to evaluate the value's range</param>
        /// <exception cref="InRangeException">Thrown when the value is not in the given range</exception>
        public static void InRange<T>(this IAssert assert, T actual, T low, T high, IComparer<T> comparer)
        {
            Guard.ArgumentNotNull("comparer", comparer);

            if (comparer.Compare(low, actual) > 0 || comparer.Compare(actual, high) > 0)
                assert.Fail(new InRangeException(actual, low, high));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that an object is of the given type or a derived type.
        /// </summary>
        /// <typeparam name="T">The type the object should be</typeparam>
        /// <param name="object">The object to be evaluated</param>
        /// <returns>The object, casted to type T when successful</returns>
        /// <exception cref="IsAssignableFromException">Thrown when the object is not the given type</exception>
        public static T IsAssignableFrom<T>(this IAssert assert, object @object)
        {
            assert.IsAssignableFrom(typeof(T), @object);
            return (T)@object;
        }

        /// <summary>
        /// Verifies that an object is of the given type or a derived type.
        /// </summary>
        /// <param name="expectedType">The type the object should be</param>
        /// <param name="object">The object to be evaluated</param>
        /// <exception cref="IsAssignableFromException">Thrown when the object is not the given type</exception>
        public static void IsAssignableFrom(this IAssert assert, Type expectedType, object @object)
        {
            Guard.ArgumentNotNull("expectedType", expectedType);

            if (@object == null || !expectedType.CanAssignFrom(@object.GetType()))
                assert.Fail(new IsAssignableFromException(expectedType, @object));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that an object is not exactly the given type.
        /// </summary>
        /// <typeparam name="T">The type the object should not be</typeparam>
        /// <param name="object">The object to be evaluated</param>
        /// <exception cref="IsNotTypeException">Thrown when the object is the given type</exception>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The generic version is a more convenient shorthand than typeof")]
        public static void IsNotType<T>(this IAssert assert, object @object)
        {
            assert.IsNotType(typeof(T), @object);
        }

        /// <summary>
        /// Verifies that an object is not exactly the given type.
        /// </summary>
        /// <param name="expectedType">The type the object should not be</param>
        /// <param name="object">The object to be evaluated</param>
        /// <exception cref="IsNotTypeException">Thrown when the object is the given type</exception>
        public static void IsNotType(this IAssert assert, Type expectedType, object @object)
        {
            Guard.ArgumentNotNull("expectedType", expectedType);

            if (@object != null && expectedType.Equals(@object.GetType()))
                assert.Fail(new IsNotTypeException(expectedType, @object));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that an object is exactly the given type (and not a derived type).
        /// </summary>
        /// <typeparam name="T">The type the object should be</typeparam>
        /// <param name="object">The object to be evaluated</param>
        /// <returns>The object, casted to type T when successful</returns>
        /// <exception cref="IsTypeException">Thrown when the object is not the given type</exception>
        public static T IsType<T>(this IAssert assert, object @object)
        {
            assert.IsType(typeof(T), @object);
            return (T)@object;
        }

        /// <summary>
        /// Verifies that an object is exactly the given type (and not a derived type).
        /// </summary>
        /// <param name="expectedType">The type the object should be</param>
        /// <param name="object">The object to be evaluated</param>
        /// <exception cref="IsTypeException">Thrown when the object is not the given type</exception>
        public static void IsType(this IAssert assert, Type expectedType, object @object)
        {
            Guard.ArgumentNotNull("expectedType", expectedType);

            if (@object == null || !expectedType.Equals(@object.GetType()))
                assert.Fail(new IsTypeException(expectedType, @object));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that a collection is not empty.
        /// </summary>
        /// <param name="collection">The collection to be inspected</param>
        /// <exception cref="ArgumentNullException">Thrown when a null collection is passed</exception>
        /// <exception cref="NotEmptyException">Thrown when the collection is empty</exception>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "object", Justification = "No can do")]
        public static void NotEmpty(this IAssert assert, IEnumerable collection)
        {
            Guard.ArgumentNotNull("collection", collection);

#pragma warning disable 168
            foreach (object @object in collection)
            {
                assert.Okay();
                return;
            }
#pragma warning restore 168

            assert.Fail(new NotEmptyException());
        }

        /// <summary>
        /// Verifies that two objects are not equal, using a default comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <exception cref="NotEqualException">Thrown when the objects are equal</exception>
        public static void NotEqual<T>(this IAssert assert, T expected, T actual)
        {
            assert.NotEqual(expected, actual, GetEqualityComparer<T>());
        }

        /// <summary>
        /// Verifies that two objects are not equal, using a custom equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <param name="comparer">The comparer used to examine the objects</param>
        /// <exception cref="NotEqualException">Thrown when the objects are equal</exception>
        public static void NotEqual<T>(this IAssert assert, T expected, T actual, IEqualityComparer<T> comparer)
        {
            Guard.ArgumentNotNull("comparer", comparer);

            if (comparer.Equals(expected, actual))
                assert.Fail(new NotEqualException());
            assert.Okay();
        }

        /// <summary>
        /// Verifies that two sequences are not equivalent, using a default comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <exception cref="NotEqualException">Thrown when the objects are equal</exception>
        public static void NotEqual<T>(this IAssert assert, IEnumerable<T> expected, IEnumerable<T> actual)
        {
            assert.NotEqual<IEnumerable<T>>(expected, actual, GetEqualityComparer<IEnumerable<T>>(true));
        }

        /// <summary>
        /// Verifies that two sequences are not equivalent, using a custom equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be compared</typeparam>
        /// <param name="expected">The expected object</param>
        /// <param name="actual">The actual object</param>
        /// <param name="comparer">The comparer used to compare the two objects</param>
        /// <exception cref="NotEqualException">Thrown when the objects are equal</exception>
        public static void NotEqual<T>(this IAssert assert, IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer)
        {
            assert.NotEqual<IEnumerable<T>>(expected, actual, GetEqualityComparer<IEnumerable<T>>(true, new AssertEqualityComparerAdapter<T>(comparer)));
        }

        /// <summary>
        /// Verifies that a value is not within a given range, using the default comparer.
        /// </summary>
        /// <typeparam name="T">The type of the value to be compared</typeparam>
        /// <param name="actual">The actual value to be evaluated</param>
        /// <param name="low">The (inclusive) low value of the range</param>
        /// <param name="high">The (inclusive) high value of the range</param>
        /// <exception cref="NotInRangeException">Thrown when the value is in the given range</exception>
        public static void NotInRange<T>(this IAssert assert, T actual, T low, T high) where T : IComparable
        {
            assert.NotInRange(actual, low, high, GetComparer<T>());
        }

        /// <summary>
        /// Verifies that a value is not within a given range, using a comparer.
        /// </summary>
        /// <typeparam name="T">The type of the value to be compared</typeparam>
        /// <param name="actual">The actual value to be evaluated</param>
        /// <param name="low">The (inclusive) low value of the range</param>
        /// <param name="high">The (inclusive) high value of the range</param>
        /// <param name="comparer">The comparer used to evaluate the value's range</param>
        /// <exception cref="NotInRangeException">Thrown when the value is in the given range</exception>
        public static void NotInRange<T>(this IAssert assert, T actual, T low, T high, IComparer<T> comparer)
        {
            Guard.ArgumentNotNull("comparer", comparer);

            if (comparer.Compare(low, actual) <= 0 && comparer.Compare(actual, high) <= 0)
                 assert.Fail(new NotInRangeException(actual, low, high));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that an object reference is not null.
        /// </summary>
        /// <param name="object">The object to be validated</param>
        /// <exception cref="NotNullException">Thrown when the object is not null</exception>
        public static void NotNull(this IAssert assert, object @object)
        {
            if (@object == null)
                 assert.Fail(new NotNullException());
            assert.Okay();
        }

        /// <summary>
        /// Verifies that two objects are not the same instance.
        /// </summary>
        /// <param name="expected">The expected object instance</param>
        /// <param name="actual">The actual object instance</param>
        /// <exception cref="NotSameException">Thrown when the objects are the same instance</exception>
        public static void NotSame(this IAssert assert, object expected, object actual)
        {
            if (object.ReferenceEquals(expected, actual))
                 assert.Fail(new NotSameException());
            assert.Okay();
        }

        /// <summary>
        /// Verifies that an object reference is null.
        /// </summary>
        /// <param name="object">The object to be inspected</param>
        /// <exception cref="NullException">Thrown when the object reference is not null</exception>
        public static void Null(this IAssert assert, object @object)
        {
            if (@object != null)
                 assert.Fail(new NullException(@object));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that the provided object raised INotifyPropertyChanged.PropertyChanged
        /// as a result of executing the given test code.
        /// </summary>
        /// <param name="object">The object which should raise the notification</param>
        /// <param name="propertyName">The property name for which the notification should be raised</param>
        /// <param name="testCode">The test code which should cause the notification to be raised</param>
        /// <exception cref="PropertyChangedException">Thrown when the notification is not raised</exception>
        public static void PropertyChanged(this IAssert assert, INotifyPropertyChanged @object, string propertyName, Action testCode)
        {
            Guard.ArgumentNotNull("object", @object);
            Guard.ArgumentNotNull("testCode", testCode);

            bool propertyChangeHappened = false;

            PropertyChangedEventHandler handler = (sender, args) =>
            {
                if (propertyName.Equals(args.PropertyName, StringComparison.OrdinalIgnoreCase))
                    propertyChangeHappened = true;
            };

            @object.PropertyChanged += handler;

            try
            {
                testCode();
                if (!propertyChangeHappened)
                     assert.Fail(new PropertyChangedException(propertyName));
                assert.Okay();
            }
            finally
            {
                @object.PropertyChanged -= handler;
            }
        }

        /// <summary>
        /// Verifies that two objects are the same instance.
        /// </summary>
        /// <param name="expected">The expected object instance</param>
        /// <param name="actual">The actual object instance</param>
        /// <exception cref="SameException">Thrown when the objects are not the same instance</exception>
        public static void Same(this IAssert assert, object expected, object actual)
        {
            if (!object.ReferenceEquals(expected, actual))
                 assert.Fail(new SameException(expected, actual));
            assert.Okay();
        }

        /// <summary>
        /// Verifies that the given collection contains only a single
        /// element of the given type.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns>The single item in the collection.</returns>
        /// <exception cref="SingleException">Thrown when the collection does not contain
        /// exactly one element.</exception>
        public static object Single(this IAssert assert, IEnumerable collection)
        {
            Guard.ArgumentNotNull("collection", collection);

            int count = 0;
            object result = null;

            foreach (object item in collection)
            {
                result = item;
                ++count;
            }

            if (count != 1)
                 assert.Fail(new SingleException(count));


            assert.Okay();
            return result;
        }

        /// <summary>
        /// Verifies that the given collection contains only a single
        /// element of the given value. The collection may or may not
        /// contain other values.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="expected">The value to find in the collection.</param>
        /// <returns>The single item in the collection.</returns>
        /// <exception cref="SingleException">Thrown when the collection does not contain
        /// exactly one element.</exception>
        public static void Single(this IAssert assert, IEnumerable collection, object expected)
        {
            Guard.ArgumentNotNull("collection", collection);

            int count = 0;

            foreach (object item in collection)
                if (Object.Equals(item, expected))
                    ++count;

            if (count != 1)
                 assert.Fail(new SingleException(count, expected));

            assert.Okay();
        }

        /// <summary>
        /// Verifies that the given collection contains only a single
        /// element of the given type.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>The single item in the collection.</returns>
        /// <exception cref="SingleException">Thrown when the collection does not contain
        /// exactly one element.</exception>
        public static T Single<T>(this IAssert assert, IEnumerable<T> collection)
        {
            return assert.Single(collection, item => true);
        }

        /// <summary>
        /// Verifies that the given collection contains only a single
        /// element of the given type which matches the given predicate. The
        /// collection may or may not contain other values which do not
        /// match the given predicate.
        /// </summary>
        /// <typeparam name="T">The collection type.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="predicate">The item matching predicate.</param>
        /// <returns>The single item in the filtered collection.</returns>
        /// <exception cref="SingleException">Thrown when the filtered collection does
        /// not contain exactly one element.</exception>
        public static T Single<T>(this IAssert assert, IEnumerable<T> collection, Predicate<T> predicate)
        {
            Guard.ArgumentNotNull("collection", collection);
            Guard.ArgumentNotNull("predicate", predicate);

            int count = 0;
            T result = default(T);

            foreach (T item in collection)
                if (predicate(item))
                {
                    result = item;
                    ++count;
                }

            if (count != 1)
                 assert.Fail(new SingleException(count));

            assert.Okay();

            return result;
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static T Throws<T>(this IAssert assert, Action testCode)
            where T : Exception
        {
            return (T)assert.Throws(typeof(T), testCode);
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// Generally used to test property accessors.
        /// </summary>
        /// <typeparam name="T">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static T Throws<T>(this IAssert assert, Func<object> testCode)
            where T : Exception
        {
            return (T)assert.Throws(typeof(T), testCode);
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// </summary>
        /// <param name="exceptionType">The type of the exception expected to be thrown</param>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static Exception Throws(this IAssert assert, Type exceptionType, Action testCode)
        {
            Guard.ArgumentNotNull("exceptionType", exceptionType);

            Exception exception = Record.Exception(testCode);

            if (exception == null)
                 assert.Fail(new ThrowsException(exceptionType));

            if (!exceptionType.Equals(exception.GetType()))
                 assert.Fail(new ThrowsException(exceptionType, exception));

            assert.Okay();

            return exception;
        }

        /// <summary>
        /// Verifies that the exact exception is thrown (and not a derived exception type).
        /// Generally used to test property accessors.
        /// </summary>
        /// <param name="exceptionType">The type of the exception expected to be thrown</param>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception cref="ThrowsException">Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static Exception Throws(this IAssert assert, Type exceptionType, Func<object> testCode)
        {
            Guard.ArgumentNotNull("exceptionType", exceptionType);

            Exception exception = Record.Exception(testCode);

            if (exception == null)
                 assert.Fail(new ThrowsException(exceptionType));

            if (!exceptionType.Equals(exception.GetType()))
                 assert.Fail(new ThrowsException(exceptionType, exception));

            assert.Okay();

            return exception;
        }

     

        

        static bool IsLineEnding(char c)
        {
            return c == '\r' || c == '\n';
        }

        static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t';
        }

        static int SkipLineEnding(string value, int index)
        {
            if (value[index] == '\r')
            {
                ++index;
            }
            if (index < value.Length && value[index] == '\n')
            {
                ++index;
            }

            return index;
        }

        static int SkipWhitespace(string value, int index)
        {
            while (index < value.Length)
            {
                switch (value[index])
                {
                    case ' ':
                    case '\t':
                        index++;
                        break;

                    default:
                        return index;
                }
            }

            return index;
        }

        class AssertEqualityComparerAdapter<T> : IEqualityComparer
        {
            readonly IEqualityComparer<T> innerComparer;

            public AssertEqualityComparerAdapter(IEqualityComparer<T> innerComparer)
            {
                this.innerComparer = innerComparer;
            }

            public new bool Equals(object x, object y)
            {
                return innerComparer.Equals((T)x, (T)y);
            }

            public int GetHashCode(object obj)
            {
                throw new NotImplementedException();
            }
        }

        class AssertEqualityComparer<T> : IEqualityComparer<T>
        {
            static IEqualityComparer defaultInnerComparer = new AssertEqualityComparerAdapter<object>(new AssertEqualityComparer<object>());
            IEqualityComparer innerComparer;
            bool skipTypeCheck;

            public AssertEqualityComparer(bool skipTypeCheck = false, IEqualityComparer innerComparer = null)
            {
                this.skipTypeCheck = skipTypeCheck;
                this.innerComparer = innerComparer ?? defaultInnerComparer;
            }

            public bool Equals(T x, T y)
            {
                Type type = typeof(T);

                // Null?
                if (type.CanBeNull())
                {
                    if (Object.Equals(x, default(T)))
                        return Object.Equals(y, default(T));

                    if (Object.Equals(y, default(T)))
                        return false;
                }

                // Same type?
                if (!skipTypeCheck && x.GetType() != y.GetType())
                    return false;

                // Implements IEquatable<T>?
                IEquatable<T> equatable = x as IEquatable<T>;
                if (equatable != null)
                    return equatable.Equals(y);

                // Implements IComparable<T>?
                IComparable<T> comparable1 = x as IComparable<T>;
                if (comparable1 != null)
                    return comparable1.CompareTo(y) == 0;

                // Implements IComparable?
                IComparable comparable2 = x as IComparable;
                if (comparable2 != null)
                    return comparable2.CompareTo(y) == 0;

                // Enumerable?
                IEnumerable enumerableX = x as IEnumerable;
                IEnumerable enumerableY = y as IEnumerable;

                if (enumerableX != null && enumerableY != null)
                {
                    IEnumerator enumeratorX = enumerableX.GetEnumerator();
                    IEnumerator enumeratorY = enumerableY.GetEnumerator();

                    while (true)
                    {
                        bool hasNextX = enumeratorX.MoveNext();
                        bool hasNextY = enumeratorY.MoveNext();

                        if (!hasNextX || !hasNextY)
                            return (hasNextX == hasNextY);

                        if (!innerComparer.Equals(enumeratorX.Current, enumeratorY.Current))
                            return false;
                    }
                }

                // Last case, rely on Object.Equals
                return Object.Equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                throw new NotImplementedException();
            }
        }

        // Only used for Assert.InRange and Assert.NotInRange
        class AssertComparer<T> : IComparer<T> where T : IComparable
        {
            public int Compare(T x, T y)
            {
                Type type = typeof(T);

                // Null?
                if (type.CanBeNull())
                {
                    if (Equals(x, default(T)))
                    {
                        if (Equals(y, default(T)))
                            return 0;
                        return -1;
                    }

                    if (Equals(y, default(T)))
                        return -1;
                }

                // Same type?
                if (x.GetType() != y.GetType())
                    return -1;

                // Implements IComparable<T>?
                IComparable<T> comparable1 = x as IComparable<T>;
                if (comparable1 != null)
                    return comparable1.CompareTo(y);

                // Implements IComparable
                return x.CompareTo(y);
            }
        }
    



    }
}
