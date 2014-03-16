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
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using ManyConsole;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Nancy.Hosting.Self;

namespace pclunit_runner
{


    public class RunConfig : ConsoleCommand
    {
        private readonly List<string> _includes = new List<string>();
        private readonly List<string> _excludes = new List<string>();
        private int? _port =null;

        public RunConfig()
        {
            IsCommand("runconfig", "run tests based on config file.");

            this.HasOption("o|output=", "Results File Output", v => _outputs.Add(WriteResults.JsonType, v));
           // this.HasOption("nunit-output=", "Results File Output in Nunit XML", v => _outputs.Add(WriteResults.NunitType, v));
            this.HasOption("noerror", "Only return error code if the test runner has error", v => { _noerrorcode = true; });
            this.HasOption("showsats", "Show windows for satellite processes", v => { _showsats = true; });
            this.HasOption("teamcity", "Team City results to Std out.", v => { PrintResults.TeamCity = true; });
            this.HasOption("appveyor", "Post results to Appveyor.", v => { PrintResults.AppVeyor = true; });

            this.HasOption<int>("port=", "Specify port to listen on", v => { _port = v; });
            this.HasOption("include=", "Include only specified assemblies, fixtures, tests or categories by uniquename",
                           v => _includes.Add(v));
            this.HasOption("exclude=", "Exclude specified assemblies, fixtures, tests or categories by uniquename",
                           v => _excludes.Add(v));
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

        private static int GetUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private IDictionary<string, Satellite> _satellites;

        private bool _noerrorcode;
        private bool _showsats;
        private Dictionary<string,string> _outputs = new Dictionary<string, string>();

        private static string PlatformFixPath(string path){
            path = path.Replace("\\", Path.DirectorySeparatorChar.ToString());
            path = path.Replace("/", Path.DirectorySeparatorChar.ToString());
            return path;
        }

        public override int Run(string[] remainingArguments)
        {
            var satpath = Path.Combine(AssemblyDirectory, "platforms.yml");

            var foundError = false;

            if (File.Exists(satpath))
            {
                using (var input = new StringReader(File.ReadAllText(satpath)))
                {
                    var des = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                    _satellites = des.Deserialize<IList<Satellite>>(input).ToDictionary(k => k.Id, v => v);
                    foreach (var satellite in _satellites)
                    {
                        satellite.Value.Path = Path.Combine(Path.GetDirectoryName(satpath),
                                                            PlatformFixPath(satellite.Value.Path));
                    }
                }
            }
            else //if no platforms.yml included, use a nested directory of exes.
            {
                _satellites = new Dictionary<string, Satellite>();
                var exeDir = Path.Combine(AssemblyDirectory, "platforms");
                var exes = Directory.GetFiles(exeDir, "*.exe");
                foreach (var exe in exes)
                {
                    var sat = new Satellite {Id = Path.GetFileName(exe), Path = Path.Combine(exeDir, exe)};
                    _satellites.Add(sat.Id, sat);
                }
            }

            if (!_satellites.Any())
            {
                Console.WriteLine("Platform Runners not found!");
                Environment.Exit(1);
            }

            string sharedpath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string configPath = remainingArguments.First();
            using (var input = new StringReader(File.ReadAllText(configPath)))
            {
                var des = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                var setting = des.Deserialize<YamlSettings>(input);

                var fullConfigPath = Path.GetDirectoryName(Path.GetFullPath(configPath));

                _port = _port ?? GetUnusedPort();

                string url = string.Format("http://localhost:{0}", _port); 
                //Create Temp shared path
                Directory.CreateDirectory(sharedpath);
               
				using (var host = new NancyHost(new HostConfiguration{RewriteLocalhost = false},new Uri(url)))
                {
                    var results = PlatformResults.Instance;
					results.ResharePath = sharedpath;
                    results.Includes.AddRange(_includes);
                    results.Excludes.AddRange(_excludes);

					host.Start();
					Console.WriteLine("Server running on {0}", url);

                    var assemblies = setting.Config.Assemblies.ToDictionary(Path.GetFileName, v => v);

                    var threadList = new List<Thread>();
                    
                        lock (results.WaitingForPlatforms)
                        {
                            //lock is not necessary but underscores that fact
                            //that the WaitingForPlatforms needs to be complete before the end of 
                            //this block.
                            foreach (var set in setting.Config.Platforms)
                            {
                                var sat = _satellites[set.Id];
                                var pgr = Path.Combine(Path.GetDirectoryName(satpath), PlatformFixPath(sat.Path));
                                var progexists = File.Exists(pgr);
                                results.WaitingForPlatforms.Add(set.Id);

                                Func<string, string> expandPath =
								it => string.Format("\"{0}\"", Path.Combine(fullConfigPath, PlatformFixPath(it)));

                                var currentAssemblies = CurrentAssemblies(assemblies, set);

                                var asmpaths = currentAssemblies
                                                      .Values
                                                      .Select(expandPath)
                                                      .Aggregate(String.Empty,
                                                                 (seed, item) => string.Format("{0} {1}", seed, item));

                                var processArgs = String.Format("sat {4} {0} {1} {2} {3}",
                                    sat.Id, url, sharedpath, asmpaths, !_showsats ? "hidden" : "show");
                                var process = new Process()
                                                  {
                                                      StartInfo =
                                                              new ProcessStartInfo(pgr, processArgs)
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
                                                                      results.Exited(sat.Id);
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

                        WriteResults.ToFiles(results.File, _outputs);
                        PrintResults.PrintEnd(results);
                        foundError = (results.Errors.Any() || results.Failures.Any());
                }
            }

            try
            {         
                //Try Delete Temp shared path
                Directory.Delete(sharedpath);
            }catch{}
           
            if (!_noerrorcode && foundError)
                return 1;
            return 0;
        }

        private static Dictionary<string, string> CurrentAssemblies(Dictionary<string, string> assemblies, Platform set)
        {
            var currentAssemblies = new Dictionary<string, string>(assemblies);

            var satAssemblies =
                (set.Assemblies ?? Enumerable.Empty<string>()).ToDictionary(Path.GetFileName, v => v);

            foreach (var satAssem in satAssemblies)
            {
                if (currentAssemblies.ContainsKey(satAssem.Key))
                    currentAssemblies[satAssem.Key] = satAssem.Value;
                else
                    currentAssemblies.Add(satAssem.Key, satAssem.Value);
            }
            return currentAssemblies;
        }
    }
		
}