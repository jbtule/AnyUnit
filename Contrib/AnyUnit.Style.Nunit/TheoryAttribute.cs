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

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// TheoryAttribute marks a test method whose parameters are populated
    /// combinatorially from data-supplying attributes on those parameters
    /// (e.g. [Values], [ValueSource], [Range]) - the same mechanism
    /// TestAttribute already uses for [TestCase]/parameter data, reused
    /// as-is here.
    ///
    /// Unlike real NUnit, a Theory with no data sources on any parameter
    /// isn't reported Inconclusive - it just falls back to a single
    /// default-arguments run, same as a plain [Test] would (see
    /// TestAttribute.ParameterSets). Not worth the extra machinery to
    /// replicate exactly for what's expected to be an edge case.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TheoryAttribute : TestAttribute
    {
    }
}
