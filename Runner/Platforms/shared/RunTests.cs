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
using System.Linq;
using System.Reflection;
using AnyUnit.Run;

namespace SatelliteRunner.Shared
{
    public partial class RunTests
    {
        public ResultsFile RunAlone(string id, IEnumerable<string> dlls)
        {
            var dllList = dlls.ToList();

            // A test assembly (e.g. FsUnitTests) can have NuGet dependencies
            // (e.g. FSharp.Core) that this runner doesn't already carry in
            // its own output directory - Assembly.LoadFrom alone won't find
            // those. Probe each test dll's own directory as a fallback.
            var probeDirs = dllList.Select(Path.GetDirectoryName).Distinct().ToList();
#if NETFRAMEWORK
            // Byte-loaded assemblies land in no context, so unlike
            // LoadFrom the CLR will not hand back an already-loaded one on
            // a repeat request - it would happily create a second copy of
            // the same assembly, which is the very problem this whole
            // branch exists to avoid. Cache by simple name.
            var probed = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
#endif
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                var candidate = probeDirs
                    .Select(dir => Path.Combine(dir, name + ".dll"))
                    .FirstOrDefault(File.Exists);
                if (candidate == null)
                    return null;
#if NETFRAMEWORK
                // Bytes, not LoadFrom - for the same reason the test
                // assemblies themselves are byte-loaded below, one level
                // deeper. A LoadFrom'd AnyUnit.Constraints.dll resolves
                // ITS OWN AnyUnit reference out of the directory it came
                // from, which is a second AnyUnit again, and the failure
                // is a step subtler than zero tests: the test passes an
                // IAssert from the embedded AnyUnit into an AssertEx.That
                // overload typed against the disk one, so it dies with
                // MissingMethodException on a method that plainly exists.
                // Confirmed for real on Windows CI before this fix.
                lock (probed)
                {
                    Assembly hit;
                    if (probed.TryGetValue(name, out hit))
                        return hit;
                    var loaded = Assembly.Load(File.ReadAllBytes(candidate));
                    probed[name] = loaded;
                    return loaded;
                }
#else
                return Assembly.LoadFrom(candidate);
#endif
            };

#if NETFRAMEWORK
            // Load the bytes rather than LoadFrom, on .NET Framework only.
            //
            // That runner ships as a single .exe with its dependencies
            // embedded (see net48-runner's DiminishedProgram), so
            // AnyUnit.dll is deliberately NOT on disk beside it. Every test
            // assembly's own output directory does carry a copy, though -
            // and under LoadFrom, the CLR satisfies that assembly's
            // AnyUnit reference from its own directory without ever raising
            // AssemblyResolve. The result is two AnyUnit assemblies with
            // different identities: the runner's [Test] attribute type is
            // not the type on the tests, so nothing matches. Confirmed for
            // real rather than theorised - every one of the 8 assemblies
            // discovered exactly 0 tests, silently, with the runner still
            // exiting 0.
            //
            // Loading the bytes puts the test assembly in no context, so
            // ALL of its dependencies go through AssemblyResolve instead:
            // the embedded copy for AnyUnit itself (handler registered at
            // startup, so it runs before the probe-dirs one above), and the
            // probe-dirs handler for anything that only exists next to the
            // test assembly, like FSharp.Core.
            //
            // Nothing here reads Assembly.Location/CodeBase (checked across
            // AnyUnit, Runner and Contrib), which is the usual thing that
            // breaks for a byte-loaded assembly.
            var am = dllList.Select(dll => Assembly.Load(File.ReadAllBytes(dll))).ToList();
#else
            // .NET (Core) resolves a dependency by simple name against
            // what the default load context has already loaded, so the
            // runner's own AnyUnit satisfies the test assembly's reference
            // and the single-file bundle needs none of the above.
            var am = dllList.Select(Assembly.LoadFrom).ToList();
#endif

            return RunAssemblies(id, am);
        }

        /// <summary>
        /// The actual create/discover/run/print core both RunAlone above
        /// (CLI use: assemblies come from LoadFrom'd file paths) and
        /// AnyUnit.Runner.Bootstrap's own Runner.RunCore (library use:
        /// assemblies are already resolved - the calling assembly, or a
        /// caller-named already-loaded one) share - only how the assembly
        /// list gets built, and what the caller does with the returned
        /// ResultsFile afterward (WriteResults.ToFiles's multi-format
        /// output here vs. a single JSON path there), differs between the
        /// two.
        /// </summary>
        public ResultsFile RunAssemblies(string id, IEnumerable<Assembly> assemblies)
        {
            var runner = Runner.Create(id, assemblies);
            PrintOutAloneStart(id);
            var file = new ResultsFile();
            runner.RunAll(r =>
                              {
                                  PrintOutAloneResults(r);
                                  file.Add(r);
                              });
            PrintOutAloneEnd(id, file);
            return file;
        }
    }
}
