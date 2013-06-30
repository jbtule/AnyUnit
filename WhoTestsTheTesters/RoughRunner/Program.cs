// 
//  Copyright 2013 PclUnit Contributors
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
using System.Text;
using PclUnit.Runner;

namespace RoughRunner
{
    public class Program
    {
        static private List<Result> _correct = new List<Result>();

        static private List<Result> _invalid = new List<Result>();

        static private List<Result> _unknown = new List<Result>(); 


        static int Main(string[] args)
        {
            
            
            var pm = new PlatformMeta()
                         {
                             Name = "net", 
                             Arch = Environment.Is64BitProcess ? "x64" : "x86",
                             Version = "40",
                             Profile = "full"
                         };
            var am = Assembly.GetAssembly(typeof(BasicTests.Basic));
            var am2 = Assembly.GetAssembly(typeof(ConstraintsTests.Basic));
               
            //using (var outfile = File.Open("platform.json", FileMode.Create))
            //using (var writer = new StreamWriter(outfile))
            //{
            //    writer.Write(pm.ToListJson());
            //}
            
            foreach (var fixtureTests in Generate.Tests(pm, new[] {am, am2}).GroupBy(t=>new{FixtureName =t.Fixture.Name, AssemblyName = t.Fixture.Assembly.Name}))
            {
                var suiteName = fixtureTests.Key.AssemblyName + "." + fixtureTests.Key.FixtureName;
                TeamCity.WriteLine("##teamcity[testSuiteStarted name='{0}']",suiteName);
                
                foreach(var test in fixtureTests){
                    var result = test.Run();
                
                    TeamCity.DontWriteLine("*************************");
            
                    TeamCity.WriteLine("##teamcity[testStarted name='{0}' captureStandardOutput='true']", result.Test.Name);
                
                    Console.WriteLine(result.Test.Name);
                    Console.Write(result.Kind);
                    Console.WriteLine(" ({0})", result.EndTime - result.StartTime);
                    Console.WriteLine(result.Output);


                    var match = ResultMatchesName(result);
                    if (!match.HasValue)
                    {
                        _unknown.Add(result);
                    
                        TeamCity.WriteLine("##teamcity[testIgnored name='{0}' message='Result type not specified']",
                          result.Test.Name);
                        Console.WriteLine("???? Unknown Output ????");
                    }else if (match.Value)
                    {
                        _correct.Add(result);
                        Console.WriteLine("++++ Correct Output ++++");
                    }
                    else
                    {
                        _invalid.Add(result);
                 
                        TeamCity.WriteLine("##teamcity[testFailed name='{0}' message='Does not match expected result' details='did not expect {1}']",result.Test.Name,result.Kind);
                
                        Console.WriteLine("xxxx Invalid Output xxxx");
                    }
                

                    TeamCity.WriteLine("##teamcity[testFinished name='{0}' duration='{1}']",result.Test.Name,(result.EndTime - result.StartTime).TotalMilliseconds);
                }
                TeamCity.WriteLine("##teamcity[testSuiteFinished name='{0}']",suiteName);
            } 
            
          
            
            Console.WriteLine();
            Console.WriteLine("*************************");
            Console.WriteLine("*************************");
            Console.WriteLine("*************************");
            Console.WriteLine();

            Console.WriteLine("Correct:{0}, Invalid:{1}, Unknown:{2}", _correct.Count, _invalid.Count, _unknown.Count); 
            Console.WriteLine();
            PrintOutItems("Invalid", _invalid); 
            Console.WriteLine();
            PrintOutItems("Unknown", _unknown, true);

            if (_invalid.Count > 0)
            {
                return -1;
            }
            return 0;
        }
        
        internal static class TeamCity{
            
            private static bool _teamCityRunner;

            static TeamCity(){
                _teamCityRunner = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            }
            
            internal static void WriteLine(string format, params object[] objs){
                if(_teamCityRunner)
                    Console.WriteLine(format, objs);
            }
            internal static void DontWriteLine(string format, params object[] objs)
            {
                if (!_teamCityRunner)
                    Console.WriteLine(format, objs);
            }
            
        }

        private static bool? ResultMatchesName(Result result)
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
        private static void PrintOutItems(string header, IList<Result> results, bool printoutAll =false)
        {
            if (results.Any())
            {
                Console.WriteLine("{0}: ", header);
                foreach (var result in results)
                {
                    Console.WriteLine("  {0}.{1} {2}", result.Test.Fixture.Name, result.Test.Name, result.Kind);
                    if (result.Kind == ResultKind.Error || printoutAll)
                    {
                        Console.WriteLine(result.Output);
                    }
                }
            }
        }
    }
}
