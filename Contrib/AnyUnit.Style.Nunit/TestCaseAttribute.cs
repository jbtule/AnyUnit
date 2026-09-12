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

using AnyUnit.Run.Attributes;

namespace AnyUnit.Style.Nunit
{
    public class TestCaseAttribute : TestAttribute, IRowInlineParameter
    {
        public TestCaseAttribute(params object[] arguments)
        {
            // [TestCase(null)] - a single null-literal argument - is a real, common
            // pattern (asserting how a method handles a null argument). C#'s params
            // rule for a single argument whose type converts directly to the array
            // type (object[], which a null literal does) passes it AS the array
            // itself rather than wrapping it - so `arguments` here is null, not
            // new object[] { null }, even though the intent is clearly "one argument,
            // which is null". Left uncorrected, Arguments (and the ParameterSet built
            // from it) would end up null instead of a one-element array, silently
            // turning a one-argument test into a zero-argument one - or worse,
            // throwing downstream wherever a null Parameters array wasn't expected.
            this.Arguments = arguments ?? new object[] { null };
        }

        public object[] Arguments { get; set; }

        /// <summary>
        /// Non-null skips just this one row, with this reason - real
        /// NUnit's per-case Ignore (e.g.
        /// [TestCase(4, Ignore = "4bpp not supported")]), distinct from
        /// a whole-method [Ignore]. Read by TestAttribute.ParameterSets
        /// into ParameterSet.IgnoreReason.
        /// </summary>
        public string Ignore { get; set; }

    }
}