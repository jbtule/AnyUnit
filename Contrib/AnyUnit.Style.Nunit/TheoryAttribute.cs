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
using System.Linq;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// TheoryAttribute marks a test method whose parameters are populated
    /// combinatorially from data-supplying attributes on those parameters
    /// (e.g. [Values], [ValueSource], [Range]) - the same mechanism
    /// TestAttribute already uses for [TestCase]/parameter data, reused
    /// as-is here.
    ///
    /// One real addition over TestAttribute's own behavior: a Theory
    /// parameter with NO explicit data-supplying attribute, if it's an
    /// enum type, gets every one of that enum's values automatically -
    /// real NUnit's own documented Theory behavior (a Theory is meant to
    /// hold for every input, so an enum parameter with nothing else said
    /// about it means "every value of this enum", not "no data" - a real,
    /// unremarkable idiom in practice, not the edge case an earlier
    /// version of this comment assumed away; confirmed by a real
    /// TargetParameterCountException from a genuine Tesseract.Tests
    /// [Theory] method ported this way, not a hypothetical).
    ///
    /// A Theory with NO parameter resolved this way at all (no attributes,
    /// no enum-typed parameters either) still falls back to TestAttribute's
    /// own single default-arguments run, same as a plain [Test] would.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TheoryAttribute : TestAttribute
    {
        public override TestParameterSetProducer ParameterSets
        {
            get
            {
                return method =>
                           {
                               var baseSets = base.ParameterSets(method).ToList();
                               var parameters = method.GetParameters();
                               // TestAttribute already found real data sources ([TestCase],
                               // [Values]/[ValueSource]/[Random] covering every parameter,
                               // etc.) - defer to that untouched; only step in when it fell
                               // all the way back to its own single, zero-argument default
                               // (checked by shape, not just count: a Theory with exactly
                               // one real combination - e.g. a single-valued [Values] - also
                               // produces exactly one ParameterSet, but with real arguments,
                               // not none).
                               var fellBackToDefault = baseSets.Count == 1 && baseSets[0].Parameters.Length == 0;
                               if (!fellBackToDefault || parameters.Length == 0 || !parameters.All(p => p.ParameterType.IsEnum))
                               {
                                   return baseSets;
                               }

                               var perParameterValues = parameters
                                   .Select(p => Enum.GetValues(p.ParameterType).Cast<object>());

                               var accum = Enumerable.Empty<System.Collections.Generic.IEnumerable<object>>();
                               accum = perParameterValues.Aggregate(accum, CombineHelper);

                               return accum.Select(v => new ParameterSet(v.ToArray())).ToList();
                           };
            }
        }
    }
}
