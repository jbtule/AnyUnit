using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SatelliteRunner.Shared;

namespace AnyUnit.Runner.Bootstrap
{
    /// <summary>
    /// The library form of anyunit-runner's own "point it at a .dll and
    /// run" behavior, narrowed to two cases: discover and run whatever
    /// tests are in the CALLING assembly (the common case - see this
    /// project's own .csproj comment for the full rationale and worked
    /// examples), or in a caller-supplied list of assemblies already
    /// loaded/linked into the process. Called directly from a consumer's
    /// own entry point instead of a generated/hand-rolled Main.
    /// </summary>
    public static class Runner
    {
        /// <summary>
        /// Discovers and runs every AnyUnit test in the assembly that
        /// calls this method, printing the same human-readable (or, with
        /// <paramref name="teamCity"/>, TeamCity service-message) output
        /// anyunit-runner's own console output uses. <paramref name="platform"/>
        /// is a free-form label (shows up in each Result's own Platform
        /// field and in the printed output) - not interpreted by AnyUnit
        /// itself, just like anyunit-runner's own RunnerId.
        /// </summary>
        /// <param name="platform">Free-form platform label, e.g. "net10", "browser-wasm".</param>
        /// <param name="teamCity">TeamCity service messages instead of the plain human-readable summary.</param>
        /// <param name="jsonOutputPath">If set, the full results are also written here as JSON (same shape anyunit-runner's own `-o`/`-output` flag produces).</param>
        /// <returns>0 if every test passed (no Fail/Error results); 1 otherwise - suitable as a process exit code.</returns>
        public static int Run(string platform, bool teamCity = false, string jsonOutputPath = null)
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return RunCore(platform, new[] { callingAssembly }, teamCity, jsonOutputPath);
        }

        /// <summary>
        /// Same as <see cref="Run(string, bool, string)"/>, but discovers tests
        /// across a caller-named set of assemblies instead of just the calling
        /// one - for a host that statically links/bundles several test
        /// assemblies together (e.g. a browser-wasm build compiling in more
        /// than one project's tests) rather than compiling them all into one.
        /// Each name is matched by simple assembly name (no version/culture/
        /// public key token) against whatever's already loaded into the
        /// process (<see cref="AppDomain.CurrentDomain"/> - in a wasm build,
        /// that's every assembly the runtime loaded at boot, native-linked or
        /// not); a name not found there is tried via <see cref="Assembly.Load(string)"/>
        /// as a fallback, for a host where it isn't necessarily preloaded.
        /// </summary>
        /// <param name="platform">Free-form platform label, e.g. "net10", "browser-wasm".</param>
        /// <param name="assemblyNames">Simple names of the assemblies to discover tests in (e.g. "Tesseract.Tests", "Tesseract.Tests.SkiaSharp").</param>
        /// <param name="teamCity">TeamCity service messages instead of the plain human-readable summary.</param>
        /// <param name="jsonOutputPath">If set, the full results are also written here as JSON (same shape anyunit-runner's own `-o`/`-output` flag produces).</param>
        /// <returns>0 if every test passed (no Fail/Error results); 1 otherwise - suitable as a process exit code.</returns>
        /// <exception cref="InvalidOperationException">A named assembly isn't loaded and couldn't be loaded either - almost always means it's not actually linked/referenced into this build.</exception>
        public static int Run(string platform, IEnumerable<string> assemblyNames, bool teamCity = false, string jsonOutputPath = null)
        {
            var assemblies = ResolveAssemblies(assemblyNames);
            return RunCore(platform, assemblies, teamCity, jsonOutputPath);
        }

        private static Assembly[] ResolveAssemblies(IEnumerable<string> assemblyNames)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies()
                .GroupBy(a => a.GetName().Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var resolved = new List<Assembly>();
            foreach (var name in assemblyNames)
            {
                Assembly assembly;
                if (!loaded.TryGetValue(name, out assembly))
                {
                    try
                    {
                        assembly = Assembly.Load(name);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            string.Format("No loaded or loadable assembly named '{0}' - is it actually linked/referenced into this build?", name), ex);
                    }
                }
                resolved.Add(assembly);
            }
            return resolved.ToArray();
        }

        private static int RunCore(string platform, Assembly[] assemblies, bool teamCity, string jsonOutputPath)
        {
            // RunTests.TeamCity is a static field (shared with anyunit-runner's
            // own use of RunAssemblies/the same PrintOutAlone* methods, see
            // Platforms/shared/RunAloneCommand.cs) - fine for this library's own
            // contract (one Run call = one whole test run per process), just
            // not something to set concurrently from two Run calls in the same
            // process.
            RunTests.TeamCity = teamCity;
            var file = new RunTests().RunAssemblies(platform, assemblies);

            if (jsonOutputPath != null)
            {
                File.WriteAllText(jsonOutputPath, file.ToListJson());
            }

            return file.HasError ? 1 : 0;
        }
    }
}
