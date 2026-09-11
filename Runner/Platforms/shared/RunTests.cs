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
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name).Name;
                var candidate = probeDirs
                    .Select(dir => Path.Combine(dir, name + ".dll"))
                    .FirstOrDefault(File.Exists);
                return candidate != null ? Assembly.LoadFrom(candidate) : null;
            };

            var am = dllList.Select(Assembly.LoadFrom).ToList();

            var runner = Runner.Create(id, am);
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
