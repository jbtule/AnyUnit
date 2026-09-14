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

using System;

namespace AnyUnit.Run.Attributes
{
    /// <summary>
    /// Declares that a test (or every test in a fixture) needs a runtime
    /// facility that not every platform has. Where it is missing the test
    /// is reported Ignored, with the reason, instead of failing for a
    /// reason that has nothing to do with the code under test.
    /// </summary>
    /// <remarks>
    /// Lives in core rather than in a style package, and is read by the
    /// engine rather than by a host, which is the whole point: one
    /// attribute works in every style at once, and every runner honours it
    /// without knowing it exists. The thing it replaces - the browser-wasm
    /// host excluding two hard-coded category strings - worked in exactly
    /// one host, silently swallowed any consumer's identically-named
    /// category, and left no trace in the report of why a test vanished.
    ///
    /// A style whose tests are not attributed methods at all (AnyUnit.Style.
    /// FSharp's `test { }` values, whose attributes land on a compiler-
    /// generated property rather than a reflectable method - see that
    /// style's Discovery.fs) can supply the same information by overriding
    /// TestAttributeBase.GetRequiredCapabilities instead.
    ///
    /// AllowMultiple so requirements can be stated one per line where that
    /// reads better than OR-ing flags together; the engine unions them.
    /// Inherited so a base fixture can state a requirement once.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class,
                    AllowMultiple = true, Inherited = true)]
    public class RequiresCapabilityAttribute : Attribute
    {
        public RequiresCapabilityAttribute(TestCapabilities required)
        {
            Required = required;
        }

        public TestCapabilities Required { get; private set; }
    }
}
