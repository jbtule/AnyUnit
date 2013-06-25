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

            foreach (var test in Generate.Tests(pm, new[] {am, am2}))
            {
                var result = test.Run();
                Console.WriteLine("*************************");
                Console.WriteLine(result.Test.Name);
                Console.WriteLine(result.Kind);
                Console.WriteLine(result.Output);

                var match = ResultMatchesName(result);
                if (!match.HasValue)
                {
                    _unknown.Add(result);
                    Console.WriteLine("???? Unknown Output ????");
                }else if (match.Value)
                {
                    _correct.Add(result);
                    Console.WriteLine("++++ Correct Output ++++");
                }
                else
                {
                    _invalid.Add(result);
                    Console.WriteLine("xxxx Invalid Output xxxx");
                }
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
