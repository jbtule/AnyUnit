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
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace AnyUnit.TestingPlatform
{
    internal sealed class AnyUnitTestFrameworkCapabilities : ITestFrameworkCapabilities
    {
        public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => Array.Empty<ITestFrameworkCapability>();
    }

    /// <summary>
    /// The one line a test project's Program.cs needs:
    ///   var builder = await TestApplication.CreateBuilderAsync(args);
    ///   builder.AddAnyUnitTestFramework();
    ///   using var app = await builder.BuildAsync();
    ///   return await app.RunAsync();
    /// </summary>
    public static class AnyUnitTestFrameworkExtensions
    {
        /// <summary>
        /// `testAssemblies`: which assembly(s) to discover/run tests from.
        /// Omit it when the test project compiles its own tests directly
        /// into the MTP executable (defaults to the entry assembly). Pass
        /// one explicitly when this host project instead references its
        /// test project(s) as libraries, e.g.
        /// `builder.AddAnyUnitTestFramework(typeof(BasicTests.Basic).Assembly)`.
        /// </summary>
        public static void AddAnyUnitTestFramework(this ITestApplicationBuilder builder, params Assembly[] testAssemblies)
        {
            builder.RegisterTestFramework(
                _ => new AnyUnitTestFrameworkCapabilities(),
                (capabilities, serviceProvider) => new AnyUnitTestFramework(testAssemblies));
        }
    }
}
