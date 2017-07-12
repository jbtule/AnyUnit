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
using System.Text;
using ConventionTestProcessor;
using AnyUnit.Run;

namespace RoughRunner
{
    public class Program
    {
        private static int Main(string[] args)
        {
            #if NET45 
            var platform = "net40";
            #elif NETCOREAPP2_0
            var platform = "core2_0";
            #endif

            var id = platform + (Environment.Is64BitProcess ? "x64" : "x86");

            //Just hard code in assemblies to test
            var asms = new[]
                           {
                               Assembly.GetAssembly(typeof (BasicTests.Basic)),
                               Assembly.GetAssembly(typeof (ConstraintsTests.Basic)),
                               Assembly.GetAssembly(typeof (XunitTests.Basic)),
                               Assembly.GetAssembly(typeof (NunitTests.Basic)),
                               Assembly.GetAssembly(typeof (FsUnitTests.BasicTests)),


                           };

            var runner = Runner.Create(id, asms);

            foreach (var asm in runner.Assemblies)
            {
                using (CIResultOutput.WriteSuite(asm.Name))
                    foreach (var fix in asm.Fixtures)
                    {
                        using (CIResultOutput.WriteSuite(fix.Name))
                            foreach (Test test in fix.Tests)
                            {
                                var result = test.Run(id);
                                ConventionMatch.PrintOutResult(result);
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
}
