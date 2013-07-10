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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using ManyConsole;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using PclUnit.Runner;
using YamlDotNet.RepresentationModel;
using YamlDotNet.RepresentationModel.Serialization;
using YamlDotNet.RepresentationModel.Serialization.NamingConventions;

namespace pclunit_runner
{


    public class RunConfigCommand : ConsoleCommand
    {
        public RunConfigCommand()
        {
            IsCommand("runconfig", "run tests based on config file.");

            this.HasOption("o|output=", "Results File Output", v => { _outputLocation = v; });
            this.HasOption("noerror", "Only return error code if the test runner has error", v => { _noerrorcode = true; });
            this.HasOption("showsats", "Show windows for satellite processes", v => { _showsats = true; });
            HasAdditionalArguments(1, " configFile");
        }


     
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }


        private IDictionary<string, Satellite> _satellites;
        private string _outputLocation
            ;

        private bool _noerrorcode;
        private bool _showsats;

        private static string PlatformFixPath(string path){
            path = path.Replace("\\", Path.DirectorySeparatorChar.ToString());
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            return path;
        }


        public override int Run(string[] remainingArguments)
        {
            var satpath = Path.Combine(AssemblyDirectory, "satellites.yml");


            using (var input = new StringReader(File.ReadAllText(satpath)))
            {
                var des = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                _satellites = des.Deserialize<IList<Satellite>>(input).ToDictionary(k => k.Id, v => v);
            }

            string sharedpath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string configPath = remainingArguments.First();
            using (var input = new StringReader(File.ReadAllText(configPath)))
            {
                var des = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                var setting = des.Deserialize<YamlSettings>(input);

                var fullConfigPath = Path.GetDirectoryName(Path.GetFullPath(configPath));

                // This will *ONLY* bind to localhost, if you want to bind to all addresses
                // use http://*:8080 to bind to all addresses. 
                // See http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx for more info
                string url = "http://localhost:8989";
            
                //Create Temp shared path
                Directory.CreateDirectory(sharedpath);

                using (WebApp.Start<Startup>(url))
                using (Reshare.Start(url,sharedpath))
                {

                    Console.WriteLine("Server running on {0}", url);


                    var threadList = new List<Thread>();
                    
                        lock (PlatformResult.WaitingForPlatforms)
                        {
                            //lock is not be necessary but underscores that fact
                            //that the WaitingForPlatforms needs to be complete before the end of 
                            //this block.
                            foreach (var set in setting.Config.Platforms)
                            {
                                var sat = _satellites[set.Id];

                                var pgr = Path.Combine(Path.GetDirectoryName(satpath), PlatformFixPath(sat.Path));

                                var progexists = File.Exists(pgr);

                                PlatformResult.WaitingForPlatforms.Add(set.Id);


                                Func<string, string> expandPath =
                                    it => string.Format("\"{0}\"", Path.Combine(fullConfigPath, it));

                                var asmpaths = setting.Config.Assemblies
                                                      .Select(expandPath)
                                                      .Concat(
                                                          (set.Assemblies ?? Enumerable.Empty<string>()).Select(
                                                              expandPath))
                                                      .Aggregate(String.Empty,
                                                                 (seed, item) => string.Format("{0} {1}", seed, item));

                                var process = new Process()
                                                  {
                                                      StartInfo =
                                                              new ProcessStartInfo(pgr,
                                                                               String.Format("{4} {0} {1} {2} {3}", sat.Id,
                                                                                             url, sharedpath, asmpaths, !_showsats ? "hidden": "show"))
                                                              {
                                                                  CreateNoWindow = !_showsats,
                                                                  UseShellExecute = _showsats,
                                                              }
                                                  };

                                threadList.Add(new Thread(() =>
                                                              {
                                                                  using (process)
                                                                  {
                                                                      if (progexists)
                                                                      {
                                                                          process.Start();
                                                                          process.WaitForExit();
                                                                      }
                                                                      PlatformResult.Exited(sat.Id);
                                                                  }
                                                              }));

                            }
                        }

                        foreach (var thread in threadList)
                        {
                            thread.Start();
                        }
                        foreach (var thread in threadList)
                        {
                            thread.Join();
                        }
                }
            }

            if (!String.IsNullOrWhiteSpace(_outputLocation))
            {
                using (var stream = File.Open(_outputLocation, FileMode.Create))
                using(var writer = new StreamWriter(stream))
                {
                    writer.Write( PlatformResult.File.ToListJson());
                }
            }
            
            Console.WriteLine("");
            Console.WriteLine("*******************");
            Console.WriteLine("*******************");
            Console.WriteLine("*******************");
            Console.WriteLine("");



            PrintCount(PlatformResult.Errors, "Errors:");
            PrintCount(PlatformResult.Failures, "Failures:");
            PrintCount(PlatformResult.Ignores, "Ignores:");
            PrintCount(PlatformResult.NoErrors, "NoErrors:");
            PrintTotaledCount(PlatformResult.Success, "Success:");
           
            try
            {         
                //Try Delete Temp shared path
                Directory.Delete(sharedpath);
            }catch{}

            if (!_noerrorcode && (PlatformResult.Errors.Any() || PlatformResult.Failures.Any()))
                return -1;
            return 0;
        }

     

        public static void PrintTotaledCount(IEnumerable<Result> results, string header)
        {

            lock (PlatformResult.ExpectedTests)
            {
                Console.WriteLine(header);
                var totalCount = PlatformResult.ExpectedTests.SelectMany(it => it.Value)
                                               .Select(it => it.Result)
                                               .ToLookup(it => it.Platform)
                                               .ToDictionary(k => k.Key, v => v.Count());
                foreach (var result in results.GroupBy(it => it.Platform))
                {
                    Console.Write("  " + result.Key);
                    Console.Write(":   ");
                    Console.Write(result.Count());
                    Console.Write("/");
                    if (totalCount.ContainsKey(result.Key))
                    {
                        Console.Write(totalCount[result.Key]);
                    }
                    else
                    {
                        Console.Write("0");
                    }
                    Console.WriteLine();
                }

                Console.Write("  Total");
                Console.Write(":   ");
                Console.Write(results.Count());
                Console.Write("/");
                Console.Write(totalCount.Sum(it => it.Value));
                Console.WriteLine();

                Console.WriteLine();
            }
        }

        public static void PrintCount(IEnumerable<Result> results, string header)
        {
            if (results.Any())
            {
                Console.WriteLine(header);
                foreach (var result in results.GroupBy(it => it.Platform))
                {
                    Console.Write("  "+result.Key);
                    Console.Write(":   ");
                    Console.Write(result.Count());
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
        }

    }


    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Turn cross domain on 
            var config = new HubConfiguration {EnableCrossDomain = true, EnableDetailedErrors = true};
            // This will map out to http://localhost:8989/signalr by default
            app.MapHubs(config);


         
        }
    }
}