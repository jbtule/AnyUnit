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

using System.Reflection;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// Implement on a custom attribute (applied to a test method and/or
    /// its fixture class) to run code immediately before and after every
    /// test it applies to - the same wrapping TestAttribute already does
    /// internally for [SetUp]/[TearDown], exposed so third-party
    /// attributes can hook into it too (e.g. to skip a test based on some
    /// runtime condition, by throwing AnyUnit.Run.IgnoreException from
    /// BeforeTest, before any real work happens).
    ///
    /// Close to real NUnit's ITestAction, but BeforeTest/AfterTest take
    /// the test's MethodInfo rather than NUnit's richer ITest - AnyUnit
    /// doesn't have an equivalent type at this layer. Only "runs around
    /// every test" (NUnit's ActionTargets.Test) is supported, not "runs
    /// once for the whole fixture" (ActionTargets.Suite - that's
    /// [OneTimeSetUp]/[OneTimeTearDown]'s job here).
    ///
    /// Class-level attributes run outermost: BeforeTest fires class-level
    /// actions before method-level ones, AfterTest fires method-level
    /// actions before class-level ones.
    /// </summary>
    public interface ITestAction
    {
        void BeforeTest(MethodInfo method);
        void AfterTest(MethodInfo method);
    }
}
