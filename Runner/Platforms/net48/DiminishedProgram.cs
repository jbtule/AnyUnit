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
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SatelliteRunner.Net48
{
    // net48-only entry point (see net48-runner.csproj's StartupObject),
    // which exists so this runner can ship as one .exe with nothing beside
    // it - what the net10 runner gets for free from a self-contained
    // single-file publish, and classic .NET Framework has no equivalent of.
    // Every dependency is embedded as a zip resource by the csproj and
    // handed to the CLR from here on demand.
    //
    // The approach (embed, then serve from an AssemblyResolve hook) is
    // jbtule/diminish-dependencies, reimplemented on the BCL's own
    // ZipArchive rather than vendoring that project's LZMA codec: its
    // runtime half ships as content/*.cs.pp, a packages.config-era model
    // PackageReference ignores outright, and its compressor half is a
    // Windows-only .NET Framework 4.0 exe that would have broken
    // `dotnet build` of this project on macOS/Linux. Deflate costs about
    // 18KB against LZMA here, for no vendored third-party source at all.
    //
    // Deliberately NOT ILMerge/ILRepack, which is the obvious tool for
    // "make it one file" and the wrong one here: those merge dependencies'
    // types INTO this assembly, changing which assembly those types live
    // in. This is a test runner - it reflects over test assemblies
    // compiled against AnyUnit.dll (strong-named, see
    // Directory.Build.targets), so at runtime they ask the CLR for that
    // exact identity. Merged, that reference resolves to nothing, and
    // anything that did resolve would be a different type identity than
    // this process's own. Embedding keeps each assembly's identity intact.
    internal static class DiminishedProgram
    {
        // Matches the LogicalName the csproj's EmbedDependencies target
        // gives the generated zip.
        private const string DependencyResource = "AnyUnit.Net48Runner.Dependencies.zip";

        private static readonly Dictionary<string, Assembly> Loaded =
            new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        // Nothing in this method may touch a type from an embedded
        // assembly, even indirectly: the JIT resolves every type a method
        // body references when it compiles that method, which happens
        // BEFORE its first statement runs - so a reference here would need
        // the resolver that this very method is trying to install. That is
        // why the real entry point is reached through Run() below rather
        // than inlined here, and why the shared Program.Main (which does
        // reference ManyConsole types directly) can't simply install the
        // handler itself.
        private static int Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            return Run(args);
        }

        // NoInlining for the reason above - inlined into Main, its
        // references would be JIT-resolved before the handler is attached,
        // which is exactly the failure this whole arrangement avoids. It
        // reaches the shared entry point by reflection rather than calling
        // it directly, so even this method names no embedded type.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run(string[] args)
        {
            var program = typeof(DiminishedProgram).Assembly
                .GetType("SatelliteRunner.Shared.Program", throwOnError: true);
            var main = program.GetMethod("Main",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (main == null)
                throw new MissingMethodException("SatelliteRunner.Shared.Program", "Main");
            return (int)main.Invoke(null, new object[] { args });
        }

        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;

            // Satellite/resource assemblies are asked for by name too, and
            // aren't embedded - letting the CLR carry on with its normal
            // probing is the right answer for those, not an exception.
            if (name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            lock (Loaded)
            {
                Assembly cached;
                if (Loaded.TryGetValue(name, out cached))
                    return cached;

                var bytes = ReadEmbedded(name + ".dll");
                if (bytes == null)
                    return null;

                // Load(byte[]) rather than LoadFrom: there is no file to
                // load from, and the identity carried in the image itself
                // (including the strong name) is preserved, which is the
                // whole point - see the type comment above.
                var assembly = Assembly.Load(bytes);
                Loaded[name] = assembly;
                return assembly;
            }
        }

        private static byte[] ReadEmbedded(string entryName)
        {
            var self = typeof(DiminishedProgram).Assembly;
            using (var resource = self.GetManifestResourceStream(DependencyResource))
            {
                if (resource == null)
                    return null;

                using (var archive = new ZipArchive(resource, ZipArchiveMode.Read))
                {
                    var entry = archive.GetEntry(entryName);
                    if (entry == null)
                        return null;

                    using (var entryStream = entry.Open())
                    using (var buffer = new MemoryStream())
                    {
                        entryStream.CopyTo(buffer);
                        return buffer.ToArray();
                    }
                }
            }
        }
    }
}
