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
using System.Text.RegularExpressions;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// Instance stand-in for MSTest's static StringAssert class, reached as
    /// the bare name `StringAssert` through AssertionHelper's declared
    /// property of that name - see AssertionHelper.cs for why it has to be
    /// a declared property rather than anything extension-based.
    ///
    /// The type is named StringAssertions, not StringAssert, purely so the
    /// property can be named StringAssert without leaning on C#'s
    /// "Color Color" same-name-type-and-member rule. The property name is
    /// what ported code writes; the type name is an implementation detail
    /// nobody spells.
    /// </summary>
    public class StringAssertions
    {
        private readonly IAssert _assert;

        public StringAssertions(IAssert assert)
        {
            _assert = assert;
        }

        /// <summary>Asserts `value` contains `substring`.</summary>
        public void Contains(string value, string substring, string message = null)
        {
            if (value != null && substring != null && value.IndexOf(substring, StringComparison.Ordinal) >= 0)
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("StringAssert.Contains failed. String:<{0}>. Substring:<{1}>.",
                              AssertEx.Describe(value), AssertEx.Describe(substring)), message));
        }

        /// <summary>Asserts `value` does not contain `substring`.</summary>
        public void DoesNotContain(string value, string substring, string message = null)
        {
            if (value == null || substring == null || value.IndexOf(substring, StringComparison.Ordinal) < 0)
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("StringAssert.DoesNotContain failed. String:<{0}>. Substring:<{1}>.",
                              AssertEx.Describe(value), AssertEx.Describe(substring)), message));
        }

        /// <summary>Asserts `value` starts with `substring`.</summary>
        public void StartsWith(string value, string substring, string message = null)
        {
            if (value != null && substring != null && value.StartsWith(substring, StringComparison.Ordinal))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("StringAssert.StartsWith failed. String:<{0}>. Substring:<{1}>.",
                              AssertEx.Describe(value), AssertEx.Describe(substring)), message));
        }

        /// <summary>Asserts `value` ends with `substring`.</summary>
        public void EndsWith(string value, string substring, string message = null)
        {
            if (value != null && substring != null && value.EndsWith(substring, StringComparison.Ordinal))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("StringAssert.EndsWith failed. String:<{0}>. Substring:<{1}>.",
                              AssertEx.Describe(value), AssertEx.Describe(substring)), message));
        }

        /// <summary>Asserts `value` matches `pattern`.</summary>
        public void Matches(string value, Regex pattern, string message = null)
        {
            if (value != null && pattern != null && pattern.IsMatch(value))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("StringAssert.Matches failed. String:<{0}>. Pattern:<{1}>.",
                              AssertEx.Describe(value), pattern), message));
        }

        /// <summary>Asserts `value` does not match `pattern`.</summary>
        public void DoesNotMatch(string value, Regex pattern, string message = null)
        {
            if (value == null || pattern == null || !pattern.IsMatch(value))
            {
                _assert.Okay();
                return;
            }
            _assert.Fail(AssertEx.WithMessage(
                string.Format("StringAssert.DoesNotMatch failed. String:<{0}>. Pattern:<{1}>.",
                              AssertEx.Describe(value), pattern), message));
        }
    }
}
