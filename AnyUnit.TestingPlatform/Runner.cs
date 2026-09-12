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
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;

namespace AnyUnit.TestingPlatform
{
    /// <summary>
    /// The real body behind EnableAnyUnitRunner's generated Main
    /// (CreateBuilderAsync/AddAnyUnitTestFramework/BuildAsync/RunAsync) as
    /// one real, compiled static method, not hand-woven generated text -
    /// AnyUnit.TestingPlatform.targets' own codegen now just calls this.
    ///
    /// Since it's real code (not MSBuild-generated text - that codegen is
    /// C#-only, see that target's own comment), an F# project can call it
    /// directly from its own hand-written entry point too, for a real MTP/
    /// `dotnet test`-integrated Program.fs - matching AnyUnit.Runner.
    /// Bootstrap.Runner.Run's naming, but MTP-hosted instead of a plain
    /// console runner:
    ///
    ///     [&lt;EntryPoint&gt;]
    ///     let main args =
    ///         AnyUnit.TestingPlatform.Runner.RunAsync(args).GetAwaiter().GetResult()
    /// </summary>
    public static class Runner
    {
        /// <summary>
        /// `testAssemblies`: which assembly(s) to discover/run tests from -
        /// same meaning as <see cref="AnyUnitTestFrameworkExtensions.AddAnyUnitTestFramework"/>'s
        /// own parameter (omit it to test the calling project's own entry
        /// assembly).
        /// </summary>
        public static async Task<int> RunAsync(string[] args, params Assembly[] testAssemblies)
        {
            var builder = await TestApplication.CreateBuilderAsync(args);
            builder.AddAnyUnitTestFramework(testAssemblies);
            using var app = await builder.BuildAsync();
            return await app.RunAsync();
        }
    }
}
