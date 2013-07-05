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
using System.Reflection;
using System.Text;
using ManyConsole;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json;
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
            HasAdditionalArguments(1, " configFile");
        }


        public class YamlSettings
        {
            public Config Config { get; set; }
        }

        public class Satellite
        {
            public Satellite()
            {
                Tests = new List<TestMeta>();
            }

            public string Path { get; set; }
            public string Id { get; set; }
            public IList<TestMeta> Tests { get; set; }
            public bool Connected { get; set; }
        }

        public class Config
        {
            public IList<string> Assemblies { get; set; }
            public IList<Platform> Platforms { get; set; }
        }
        public class Platform
        {
             public string Id { get; set; }
             public IList<string> Assemblies { get; set; }
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


                using (WebApp.Start<Startup>(url))
                {
                    Console.WriteLine("Server running on {0}", url);


                    var processList = new List<Process>();
                    try
                    {
                        lock (PlatformResult.WaitingForPlatforms)
                        {
                            //lock is not be necessary but underscores that fact
                            //that the WaitingForPlatforms needs to be complete before the end of 
                            //this block.
                            foreach (var set in setting.Config.Platforms)
                            {
                                var sat = _satellites[set.Id];

                                var pgr = Path.Combine(Path.GetDirectoryName(satpath), PlatformFixPath(sat.Path));

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
                                                                               String.Format("{0} {1} {2}", sat.Id,
                                                                                             url, asmpaths))
                                                              {
                                                                  CreateNoWindow = true,
                                                                  UseShellExecute = false,
                                                              }
                                                  };

                                processList.Add(process);
                            }
                        }

                        foreach (var process1 in processList)
                        {
                            process1.Start();
                        }
                        foreach (var process1 in processList)
                        {
                            process1.WaitForExit();
                        }
                    }
                    finally
                    {
                        foreach (var process1 in processList)
                        {
                            process1.Dispose();
                        }
                    }
                }
            }


            return 0;
        }
    }


    internal class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Turn cross domain on 
            var config = new HubConfiguration {EnableCrossDomain = true};
            // This will map out to http://localhost:8989/signalr by default
            app.MapHubs(config);
        }
    }


    public class PlatformResult
    {
        public PlatformResult(PlatformMeta platform)
        {
            Platform = platform;
        }
        public PlatformMeta Platform { get; set; }
        public Result Result { get; set; }

        public static readonly IDictionary<string, IList<PlatformResult>> ExpectedTests =
           new Dictionary<string, IList<PlatformResult>>();


        public static readonly HashSet<string> WaitingForPlatforms =
           new HashSet<string>();
    }

    public class PclUnitHub : Hub
    {
       

        public void Connect(string id)
        {
            Console.WriteLine("*******************");
            Console.WriteLine(id);
            Console.WriteLine("*******************");
        }

        public void List(string platformTotalJson)
        {
            Console.WriteLine("+++++++++++++++++++");
            var pm = Newtonsoft.Json.JsonConvert.DeserializeObject<PlatformMeta>(platformTotalJson);
            foreach (var test in pm.Assemblies.SelectMany(it=>it.Fixtures).SelectMany(it=>it.Tests))
            {
                lock (PlatformResult.ExpectedTests)
                {
                    string key = string.Format("{0}|{1}|{2}", test.Fixture.Assembly.UniqueName, test.Fixture.UniqueName,
                                               test.UniqueName);
                    if (!PlatformResult.ExpectedTests.ContainsKey(key))
                    {
                        PlatformResult.ExpectedTests.Add(key, new List<PlatformResult>());
                    }
                    PlatformResult.ExpectedTests[key].Add(new PlatformResult(pm));
                }
            }
            Console.WriteLine(pm.UniqueName);
            lock(PlatformResult.WaitingForPlatforms)
            {
                PlatformResult.WaitingForPlatforms.Remove(pm.UniqueName);
                if (!PlatformResult.WaitingForPlatforms.Any())
                {
                    Clients.All.TestsAreReady("go");
                }

            }
            Console.WriteLine("+++++++++++++++++++");
        }

        public void SendResult(string resultJson)
        {
            //  Console.WriteLine(resultJson);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(resultJson);

            string key = string.Format("{0}|{1}|{2}", result.Test.Fixture.Assembly.UniqueName, result.Test.Fixture.UniqueName, result.Test.UniqueName);

            var dict = PlatformResult.ExpectedTests[key].ToDictionary(k => k.Platform.UniqueName, v => v);

            var miniKey = result.Test.Fixture.Assembly.Platform.UniqueName;
            dict[miniKey].Result = result;


            if (dict.All(it => it.Value.Result != null))
            {
                Console.WriteLine(result.Test.Name);
                foreach(var grpResult in dict.GroupBy(it => it.Value.Result.Kind))
                {
                    Console.Write("{0}:", grpResult.Key);
                    foreach (var keyValuePair in grpResult)
                    {
                        Console.Write(" ");
                        Console.Write(keyValuePair.Value.Platform.Name);
                    }
                    Console.WriteLine();
                }
                TimeSpan span= new TimeSpan();
                foreach (var r in dict.Select(it=>it.Value.Result))
                {
                    span += (r.EndTime - r.StartTime);
                }
                Console.WriteLine("avg time:{0}", new TimeSpan(span.Ticks / dict.Count));
                foreach (var kp in dict)
                {
                    Console.WriteLine("{0}:", kp.Value.Platform.Name);
                    Console.WriteLine(kp.Value.Result.Output);
                }
               
                Console.WriteLine("===================");
            }
        }
    }
}