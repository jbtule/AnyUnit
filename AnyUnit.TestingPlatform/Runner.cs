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
            TryAddTrxReportProvider(builder);
            using var app = await builder.BuildAsync();
            return await app.RunAsync();
        }

        // Declaring ITrxReportCapability (see AnyUnitTrxReportCapability)
        // is necessary but not sufficient for a consuming test project's
        // `--report-trx` to actually work: the real .trx file generator
        // lives in Microsoft.Testing.Extensions.TrxReport (a much larger
        // package than the small Abstractions one this project itself
        // references - see that csproj's own comment) and has to be
        // explicitly wired onto the builder via its
        // TrxReportExtensions.AddTrxReportProvider(this
        // ITestApplicationBuilder) extension method. A consuming project
        // normally gets that wiring "for free" from Microsoft.Testing.
        // Platform.MSBuild's source-generated entry point (which scans
        // every referenced package's TestingPlatformBuilderHook MSBuild
        // item and calls each one) - but this project hand-builds its own
        // entry point instead (see AnyUnit.TestingPlatform.targets' own
        // comment on why), bypassing that generator entirely, so nothing
        // else would ever call it.
        //
        // Reflection, not a direct call, is what lets a consuming project
        // opt into TRX output just by adding its own PackageReference to
        // Microsoft.Testing.Extensions.TrxReport (matching how the rest of
        // the MTP ecosystem - MSTest, NUnit3, xUnit3 - treats it as an
        // optional add-on), without this project taking a permanent, hard
        // dependency on that far larger package for every consumer whether
        // they want TRX output or not. Verified end to end: with the
        // package referenced, AddTrxReportProvider is found and --report-trx
        // produces a real .trx file; without it, Type.GetType returns null
        // and this is a silent no-op.
        private static void TryAddTrxReportProvider(ITestApplicationBuilder builder)
        {
            var extensionsType = Type.GetType(
                "Microsoft.Testing.Extensions.TrxReportExtensions, Microsoft.Testing.Extensions.TrxReport",
                throwOnError: false);
            var method = extensionsType?.GetMethod("AddTrxReportProvider", new[] { typeof(ITestApplicationBuilder) });
            method?.Invoke(null, new object[] { builder });
        }
    }
}
