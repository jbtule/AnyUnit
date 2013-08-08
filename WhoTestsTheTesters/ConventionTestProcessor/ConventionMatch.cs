using System;
using System.Collections.Generic;
using System.Linq;
using PclUnit.Run;

namespace ConventionTestProcessor
{
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
            TeamCity.WriteLine("##teamcity[testStarted name='{0}__{1}' captureStandardOutput='true']", 
                                TeamCity.Encode(result.Test.Name),
                                TeamCity.Encode(result.Platform));

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

                TeamCity.WriteLine("##teamcity[testIgnored name='{0}__{1}' message='Result type not specified']",
                                      TeamCity.Encode(result.Test.Name),
                                      TeamCity.Encode(result.Platform));
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
                    "##teamcity[testFailed name='{0}_{1}' message='Does not match expected result' details='did not expect {2}']",
                    TeamCity.Encode(result.Test.Name), TeamCity.Encode(result.Platform), result.Kind);

                Console.WriteLine("!!!! Invalid Output !!!!");
            }
            TeamCity.DontWriteLine(String.Empty);
            TeamCity.DontWriteLine("*************************");

            TeamCity.WriteLine("##teamcity[testFinished name='{0}__{1}' duration='{2}']", 
                               TeamCity.Encode(result.Test.Name), 
                               TeamCity.Encode(result.Platform),
                               (result.EndTime - result.StartTime).TotalMilliseconds);
        }

        public static void ReprintOutItems(string header, IList<Result> results, bool printoutAll = false)
        {
            if (results.Any())
            {
                Console.WriteLine("{0}: ", header);
                foreach (var result in results)
                {
                    Console.WriteLine("  {4}.{0}.{1}[{2}] {3}", result.Test.Fixture.Name, result.Test.Name, result.Platform, result.Kind, result.Test.Fixture.Assembly.Name);
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
}