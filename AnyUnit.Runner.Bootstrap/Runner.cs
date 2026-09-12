using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AnyUnit.Run;

namespace AnyUnit.Runner.Bootstrap
{
    /// <summary>
    /// The library form of anyunit-runner's own "point it at a .dll and
    /// run" behavior, narrowed to exactly one case: discover and run
    /// whatever tests are in the CALLING assembly, called directly from
    /// that assembly's own entry point instead of a generated/hand-rolled
    /// Main. See this project's own .csproj comment for the full
    /// rationale and worked examples.
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
            var runner = AnyUnit.Run.Runner.Create(platform, new[] { callingAssembly });
            var file = new ResultsFile();

            PrintStart(platform, teamCity);
            runner.RunAll(result =>
            {
                file.Add(result);
                PrintResult(result, teamCity);
            });
            PrintEnd(platform, file, teamCity);

            if (jsonOutputPath != null)
            {
                File.WriteAllText(jsonOutputPath, file.ToListJson());
            }

            return file.HasError ? 1 : 0;
        }

        private static void PrintStart(string platform, bool teamCity)
        {
            if (teamCity)
            {
                Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", platform);
            }
            else
            {
                Console.WriteLine("Starting Tests for '{0}'", platform);
            }
        }

        private static void PrintResult(Result result, bool teamCity)
        {
            if (teamCity)
            {
                Console.WriteLine("##teamcity[testStarted name='{2}.{1}.{0}' captureStandardOutput='true']",
                    result.Test.Name, result.Test.Fixture.Name, result.Test.Fixture.Assembly.Name);
            }
            else
            {
                Console.Write(result.Test.Fixture.Assembly.Name + ".");
                Console.Write(result.Test.Fixture.Name + ".");
            }

            Console.Write(result.Test.Name);
            Console.WriteLine("[{0}]", result.Platform);
            Console.Write(result.Kind);
            Console.WriteLine(" ({0})", result.EndTime - result.StartTime);
            Console.WriteLine(result.Output);

            if (teamCity)
            {
                Console.WriteLine("##teamcity[testFinished name='{2}.{1}.{0}' duration='{3}']",
                    result.Test.Name,
                    result.Test.Fixture.Name,
                    result.Test.Fixture.Assembly.Name,
                    (result.EndTime - result.StartTime).TotalMilliseconds);
            }
            else
            {
                switch (result.Kind)
                {
                    case ResultKind.Success:
                        Console.WriteLine("-------------------------");
                        break;
                    case ResultKind.Error:
                        Console.WriteLine("EEEEEEEEEEEEEEEEEEEEEEEEE");
                        break;
                    case ResultKind.Fail:
                        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!");
                        break;
                    case ResultKind.NoError:
                        Console.WriteLine(".........................");
                        break;
                    case ResultKind.Ignore:
                        Console.WriteLine("?????????????????????????");
                        break;
                }

                Console.WriteLine(string.Empty);
            }
        }

        private static void PrintEnd(string platform, ResultsFile file, bool teamCity)
        {
            if (teamCity)
            {
                Console.WriteLine("##teamcity[testSuiteFinished name='{0}']", platform);
            }
            else
            {
                Console.WriteLine("Finished");
                Console.WriteLine();
                foreach (var kv in file.ResultCount.OrderBy(it => it.Key))
                {
                    Console.WriteLine("  {0,-15}{1,4}", kv.Key, kv.Value);
                }
                Console.WriteLine("{0,-17}{1,4}", "Total", file.ResultCount.Select(r => r.Value).Sum());
            }
        }
    }
}
