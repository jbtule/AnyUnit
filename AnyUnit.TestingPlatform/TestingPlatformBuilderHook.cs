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
using System.Reflection;
using Microsoft.Testing.Platform.Builder;

namespace AnyUnit.TestingPlatform
{
    // The type Microsoft.Testing.Platform.MSBuild's own official
    // entry-point generator calls automatically, once
    // build/AnyUnit.TestingPlatform.targets registers this project as a
    // TestingPlatformBuilderHook (the same generic extensibility point
    // every other MTP extension - TRX, Retry, CrashDump, Microsoft.Testing.
    // Platform.MSBuild's own PlatformOutputDevice hook, ... - uses). The
    // generator's own (real, on-disk) generated source confirms the exact
    // contract: a public static AddExtensions(ITestApplicationBuilder,
    // string[]) method, called from a generated SelfRegisteredExtensions.
    // AddSelfRegisteredExtensions alongside every other referenced
    // extension's own hook - so a consuming project gets AnyUnit's test
    // framework AND (for example) TRX output wired up together, generically,
    // with no AnyUnit-specific glue code of its own and no hand-written
    // Main. This also means the official generator's own F# support (it
    // genuinely emits real .fs source there, not just .cs - verified
    // directly) is what makes an .fsproj work through EnableAnyUnitRunner
    // exactly like any other project now, no hand-written Program.fs
    // needed at all - an earlier version of this project had one (calling
    // a since-removed AnyUnit.TestingPlatform.Runner.RunAsync directly),
    // built on the mistaken assumption that the generator was C#-only.
    public static class TestingPlatformBuilderHook
    {
        public static void AddExtensions(ITestApplicationBuilder builder, string[] args)
        {
            builder.AddAnyUnitTestFramework(TestAssembliesFromMetadata());
        }

        // AnyUnitTestAssembly items (see AnyUnit.TestingPlatform.targets)
        // become [assembly: AssemblyMetadata("AnyUnit.TestAssembly", "Foo")]
        // attributes at compile time - a plain SDK-level item, not source
        // generation, so it works identically for a .csproj or an .fsproj.
        // Read back here since AddExtensions' fixed (builder, args) shape,
        // unlike the old hand-rolled generated Main, has no room for a
        // per-project extra parameter the generator could thread through
        // at the call site.
        private static Assembly[] TestAssembliesFromMetadata()
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry == null)
                return Array.Empty<Assembly>();

            return entry.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(a => a.Key == "AnyUnit.TestAssembly" && !string.IsNullOrEmpty(a.Value))
                .Select(a => Assembly.Load(a.Value))
                .ToArray();
        }
    }
}
