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
using AnyUnit.Run.Attributes;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// One literal row of arguments for a [DataTestMethod] (or, as in
    /// modern MSTest, a plain [TestMethod]).
    ///
    /// Implements AnyUnit.Run.Attributes.IRowInlineParameter, and that is
    /// the whole cross-style interop story: an MSTest [DataTestMethod]
    /// immediately accepts xUnit's [InlineData] and NUnit's [TestCase]
    /// rows, and conversely those styles' primary attributes accept
    /// [DataRow], with no package referencing any other - only the core
    /// interface every style already references. See
    /// WhoTestsTheTesters/Tests/Style/ComboTests for the tests that hold
    /// this honest.
    ///
    /// Note what this attribute is NOT: a TestAttributeBase. NUnit's
    /// [TestCase] doubles as a primary test attribute in its own right, so
    /// a method carrying only [TestCase] is still a test; MSTest's
    /// [DataRow] does not, and a method carrying only [DataRow] is not a
    /// test in real MSTest either. Keeping it a plain Attribute preserves
    /// that, and also keeps Fixture.GetHarnesses's
    /// GetTopMostCustomAttribute&lt;TestAttributeBase&gt; from ever picking
    /// a [DataRow] as the primary attribute in declaration order.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DataRowAttribute : Attribute, IRowInlineParameter
    {
        public DataRowAttribute(params object[] arguments)
        {
            // [DataRow(null)] - one null-literal argument - is a real
            // pattern (asserting how a method handles null). C#'s params
            // rule passes a lone argument that converts directly to the
            // array type (which a null literal does) AS the array, so
            // `arguments` arrives null rather than new object[] { null },
            // silently turning a one-argument row into a zero-argument one.
            // Same correction, same reason, as Style.Nunit's
            // TestCaseAttribute - see its longer note.
            Arguments = arguments ?? new object[] { null };
        }

        public object[] Arguments { get; set; }

        /// <summary>
        /// MSTest's per-row display name. AnyUnit names a row from its
        /// arguments (see AnyUnit.Run.Test's constructor) and has no slot
        /// for an explicit one, so this is accepted and ignored rather than
        /// rejected - a ported [DataRow(1, DisplayName = "one")] compiles
        /// and runs, it just shows up under the argument-derived name.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
