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
    internal static class ServeMore
    {
        public static ServeMoreDisposable Start(string url,string dir)
        {
            var server = new ServeMoreDisposable(url,dir);
            server.Start();
            return server;
        }
    }

    internal class ServeMoreDisposable:IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly string _dir;

        public ServeMoreDisposable(string url, string dir)
        {
            // TODO: Complete member initialization
            _url = url;
            _dir = dir;
            _listener = new HttpListener();
            var prefix = _url + "/reshare/";
            _listener.Prefixes.Add(prefix);
            Console.WriteLine("Listening too:"+prefix);

        }

        public void Start()
        {
            _listener.Start();
            Serve(); 
        }


        private void Serve()
        {
            _listener.BeginGetContext(ar =>
            {
                _listener.Start();

                HttpListenerContext context;

                try
                {
                    context = _listener.EndGetContext(ar);
                }
                catch (Exception)
                {
                    return;
                }

                Serve();

                try
                {
                    var url = context.Request.RawUrl;
                    var path =url.Replace("/reshare/", "");
                    var fullpath = Path.Combine(_dir, path);
                    if (File.Exists(fullpath))
                    {

                        context.Response.ContentType = "application/octet-stream";
                        var bytes = File.ReadAllBytes(fullpath);
                        context.Response.Close(bytes,true);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.StatusDescription = "Not Found";
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Server Error";
                    context.Response.Close(Encoding.UTF8.GetBytes(ex.ToString()), true);
                }

            },
                 null);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }

    public class RunConfigCommand : ConsoleCommand
    {
        public RunConfigCommand()
        {
            IsCommand("runconfig", "run tests based on config file.");

            this.HasOption("o|output=", "Results File Output", v => { _outputLocation = v; });
            this.HasOption("noerror", "Only return error code if the test runner has error", v => { _noerrorcode = true; });
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
        private string _outputLocation
            ;

        private bool _noerrorcode;

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
                string sharedpath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(sharedpath);

                using (WebApp.Start<Startup>(url))
                using (ServeMore.Start(url,sharedpath))
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
                                                                               String.Format("{0} {1} {2} {3}", sat.Id,
                                                                                             url, sharedpath, asmpaths))
                                                              {
                                                                 // CreateNoWindow = true,
                                                                 // UseShellExecute = false,
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
           
            if (!_noerrorcode && (PlatformResult.Errors.Any() || PlatformResult.Failures.Any()))
                return -1;
            return 0;
        }

        public static void PrintTotaledCount(IEnumerable<Result> results, string header)
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


    public class PlatformResult
    {

        public  static readonly List<Result> NoErrors = new List<Result>();
        public static readonly List<Result> Success = new List<Result>();
        public static readonly List<Result> Failures = new List<Result>();
        public static readonly List<Result> Errors = new List<Result>();
        public static readonly List<Result> Ignores = new List<Result>();


        public static IDictionary<string,PlatformResult> AddResult(Result result)
        {
            File.Add(result);

            switch (result.Kind)
            {
       
                case ResultKind.Error:
                    Errors.Add(result);
                    break;
                case  ResultKind.Fail:
                    Failures.Add(result);
                    break;
                case ResultKind.Ignore:
                    Ignores.Add(result);
                    break;       
                case ResultKind.Success:
                    Success.Add(result);
                    break;
                case ResultKind.NoError:
                    NoErrors.Add(result);
                    break;
            }

            string key = string.Format("{0}|{1}|{2}", result.Test.Fixture.Assembly.UniqueName, result.Test.Fixture.UniqueName, result.Test.UniqueName);

            var dict = PlatformResult.ExpectedTests[key].ToDictionary(k => k.Platform, v => v);


            var miniKey = result.Platform;
            dict[miniKey].Result = result;

            return dict;
        }

        public PlatformResult(string platform)
        {
            Platform = platform;
        }
        public string Platform { get; set; }
        public Result Result { get; set; }

        public static readonly IDictionary<string, IList<PlatformResult>> ExpectedTests =
           new Dictionary<string, IList<PlatformResult>>();

        public static readonly ResultsFile File = new ResultsFile();

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
            var pm = Newtonsoft.Json.JsonConvert.DeserializeObject<Runner>(platformTotalJson);
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
                    PlatformResult.ExpectedTests[key].Add(new PlatformResult(pm.Platform));
                }
            }
            Console.WriteLine(pm.Platform);
            lock(PlatformResult.WaitingForPlatforms)
            {
                PlatformResult.WaitingForPlatforms.Remove(pm.Platform);
                if (!PlatformResult.WaitingForPlatforms.Any())
                {
                    Console.WriteLine("Go");
                    Clients.All.TestsAreReady("go");
                }

            }
            Console.WriteLine("+++++++++++++++++++");
        }

        public void SendResult(string resultJson)
        {
            //  Console.WriteLine(resultJson);
            var result = JsonConvert.DeserializeObject<Result>(resultJson);


            var dict = PlatformResult.AddResult(result);


            if (dict.All(it => it.Value.Result != null))
            {
                Console.Write(result.Test.Fixture.Assembly.Name + ".");
                Console.Write(result.Test.Fixture.Name + ".");
                Console.WriteLine(result.Test.Name);
                foreach(var grpResult in dict.GroupBy(it => it.Value.Result.Kind))
                {
                    Console.Write("{0}:", grpResult.Key);
                    foreach (var keyValuePair in grpResult)
                    {
                        Console.Write(" ");
                        Console.Write(keyValuePair.Value.Platform);
                    }
                    Console.WriteLine();
                }
                var span= new TimeSpan();
                foreach (var r in dict.Select(it=>it.Value.Result))
                {
                    span += (r.EndTime - r.StartTime);
                }
                Console.WriteLine("avg time:{0}", new TimeSpan(span.Ticks / dict.Count));
              

                foreach (var lup in dict.ToLookup(it => it.Value.Result.Output))
                {

                    var name = string.Join(",", lup.Select(it => it.Value.Platform));
                   
                    Console.WriteLine("{0}:", name);
                    Console.WriteLine(lup.Key);
                }
               
                Console.WriteLine("===================");
            }
        }
    }
}