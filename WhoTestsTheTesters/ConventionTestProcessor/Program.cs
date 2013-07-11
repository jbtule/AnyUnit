using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PclUnit.Runner;

namespace ConventionTestProcessor
{
    public class Program
    {


        static int Main(string[] args)
        {
            var file = args.FirstOrDefault();
            if (args.Any())
            {
                throw new ArgumentException("Missing argument to process file");
            }


            return VerifyJsonResults(args.Select(File.ReadAllText));
        }

        public static int VerifyJsonResults(IEnumerable<string> jsons)
        {

            foreach (var json in jsons)
            {
                var results = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultsFile>(json);

                foreach (var asm in results.Assemblies)
                {
                    using (TeamCity.WriteSuite(asm.Name))
                        foreach (var fix in asm.Fixtures)
                        {
                            using (TeamCity.WriteSuite(fix.Name))
                                foreach (var test in fix.Tests)
                                {
                                    foreach (var result in test.Results)
                                    {
                                        ConventionMatch.PrintOutResult(result);
                                    }
                                }
                        }
                }
            }


            ConventionMatch.WriteOutTrailer();


            if (ConventionMatch.Invalid.Count > 0)
            {
                return -1;
            }
            return 0;
        }
    }



    public static class ConventionMatch
    {
        static public List<Result> Correct = new List<Result>();

        static public List<Result> Invalid = new List<Result>();

        static public List<Result> Unknown = new List<Result>(); 

        public static void WriteOutTrailer()
        {
            Console.WriteLine();
            Console.WriteLine("*************************");
            Console.WriteLine("*************************");
            Console.WriteLine("*************************");
            Console.WriteLine();

            Console.WriteLine("Correct:{0}, Invalid:{1}, Unknown:{2}", ConventionMatch.Correct.Count, ConventionMatch.Invalid.Count, ConventionMatch.Unknown.Count);
            Console.WriteLine();
            ConventionMatch.ReprintOutItems("Invalid", ConventionMatch.Invalid);
            Console.WriteLine();
            ConventionMatch.ReprintOutItems("Unknown", ConventionMatch.Unknown, true);
        }

        public static void PrintOutResult(Result result)
        {
            TeamCity.WriteLine("##teamcity[testStarted name='{0}__{1}' captureStandardOutput='true']", result.Test.Name,
                               result.Platform);

            TeamCity.DontWrite(result.Test.Fixture.Assembly.Name + ".");
            TeamCity.DontWrite(result.Test.Fixture.Name + ".");
            Console.Write(result.Test.Name);
            Console.WriteLine("[{0}]", result.Platform);
            Console.Write(result.Kind);
            Console.WriteLine(" ({0})", result.EndTime - result.StartTime);
            Console.WriteLine(result.Output);


            var match = ConventionMatch.ResultMatchesName(result);
            if (!match.HasValue)
            {
                Unknown.Add(result);

                TeamCity.WriteLine("##teamcity[testIgnored name='{0}' message='Result type not specified']",
                                   result.Test.Name);
                Console.WriteLine("???? Unknown Output ????");
            }
            else if (match.Value)
            {
                Correct.Add(result);
                Console.WriteLine("++++ Correct Output ++++");
            }
            else
            {
                Invalid.Add(result);

                TeamCity.WriteLine(
                    "##teamcity[testFailed name='{0}' message='Does not match expected result' details='did not expect {1}']",
                    result.Test.Name, result.Kind);

                Console.WriteLine("xxxx Invalid Output xxxx");
            }
            TeamCity.DontWriteLine(String.Empty);
            TeamCity.DontWriteLine("*************************");
         
            TeamCity.WriteLine("##teamcity[testFinished name='{0}__{1}' duration='{2}']", result.Test.Name, result.Platform,
                               (result.EndTime - result.StartTime).TotalMilliseconds);
        }

        public static void ReprintOutItems(string header, IList<Result> results, bool printoutAll = false)
        {
            if (results.Any())
            {
                Console.WriteLine("{0}: ", header);
                foreach (var result in results)
                {
                    Console.WriteLine("  {0}.{1}[{2}] {3}", result.Test.Fixture.Name, result.Test.Name, result.Platform, result.Kind);
                    if (result.Kind == ResultKind.Error || printoutAll)
                    {
                        Console.WriteLine(result.Output);
                    }
                }
            }
        }

        public static bool? ResultMatchesName(Result result)
        {
            bool reverse = result.Test.Name.Contains("_Opposite");

            if (result.Test.Name.Contains("_Fail"))
            {
                if (reverse)
                    return result.Kind == ResultKind.Success;
                return result.Kind == ResultKind.Fail;
            }
            else if (result.Test.Name.Contains("_Success"))
            {
                if (reverse)
                    return result.Kind == ResultKind.Fail;
                return result.Kind == ResultKind.Success;
            }
            else if (result.Test.Name.Contains("_Ignore"))
            {
                return result.Kind == ResultKind.Ignore;
            }
            else if (result.Test.Name.Contains("_Error"))
            {
                return result.Kind == ResultKind.Error;
            }
            else if (result.Test.Name.Contains("_NoError"))
            {
                return result.Kind == ResultKind.NoError;
            }

            return null;
        }
    }

    public static class TeamCity
    {

        private static bool _teamCityRunner;

        static TeamCity()
        {
            _teamCityRunner = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
        }

        public class EndSuite:IDisposable
        {
            private readonly string _name;

            public EndSuite(string name)
            {
                _name = name;
            }


            public void Dispose()
            {
                TeamCity.WriteLine("##teamcity[testSuiteFinished name='{0}']", _name);
            }
        }

        public static bool Disable { get; set; }

        public static bool ShouldDisplay
        {
            get { return _teamCityRunner && !Disable; }
        }

        public static IDisposable WriteSuite(string name)
        {
            WriteLine("##teamcity[testSuiteStarted name='{0}']", name);
            return  new EndSuite(name);
        }


        public static void WriteLine(string format, params object[] objs)
        {
            if (ShouldDisplay)
                Console.WriteLine(format, objs);
        }
        public static void DontWriteLine(string format, params object[] objs)
        {
            if (!ShouldDisplay)
                Console.WriteLine(format, objs);
        }
        public static void DontWrite(string format, params object[] objs)
        {
            if (!ShouldDisplay)
                Console.Write(format, objs);
        }

    }


}
