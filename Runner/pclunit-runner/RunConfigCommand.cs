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
    public class RunConfigCommand:ConsoleCommand
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


        public class Platform
        {
            public string Id { get; set; }
            public string Profile { get; set; }
            public string Arch { get; set; }
            public string Version { get; set; }
            public IList<string> Assemblies { get; set; }

            public string FullId()
            {
                return String.Format("{0}-{1}-{2}-{3}", Id, Version ?? "any", Profile ?? "any", Arch ?? "any" );
            }

            public string Readable()
            {
                var profile = String.Empty;
                if (Profile != null)
                    profile = "-" + Profile;

                var arch = String.Empty;
                if (Arch != null)
                    arch = "-" + Arch;

                return String.Format("{0}{1}{2}{3}", Id, Version ?? String.Empty, profile, arch);
            }
          

        }

        public class Satellite
        {
            public Satellite()
            {
                 Tests = new List<TestMeta>();
            }

            public string Path { get; set; }
            public Platform Platform { get; set; }
            public IList<TestMeta> Tests { get; set; }
            public bool Connected { get; set; }
        }

        public class Config
        {
            public IList<string> Assemblies { get; set; }
            public IList<Platform> Platforms { get; set; }
        }

        static public string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }



        public override int Run(string[] remainingArguments)
        {

            var satpath =Path.Combine(AssemblyDirectory, "satellites.yml");

            using (var input = new StringReader(File.ReadAllText(satpath)))
            {
                var des = new Deserializer(namingConvention: new CamelCaseNamingConvention());
                var sats = des.Deserialize<IList<Satellite>>(input);
            }



           using (var input = new StringReader(File.ReadAllText(remainingArguments.First())))
           {
               var des = new Deserializer(namingConvention: new CamelCaseNamingConvention());
               var setting = des.Deserialize<YamlSettings>(input);



               // This will *ONLY* bind to localhost, if you want to bind to all addresses
               // use http://*:8080 to bind to all addresses. 
               // See http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx for more info
               string url = "http://*:8989";

               

               using (WebApplication.Start<Startup>(url))
               {
                   Console.WriteLine("Server running on {0}", url);
                   Console.ReadLine();
               }
           }




            return 0;
        }
    }


    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Turn cross domain on 
            var config = new HubConfiguration { EnableCrossDomain = true };

            // This will map out to http://localhost:8989/signalr by default
            app.MapHubs(config);
        }
    }

    public class PclUnitHub : Hub
    {
        public void Connect(string id)
        {
            Console.WriteLine("===================");
            Console.WriteLine(id);
            Console.WriteLine("===================");
        }

        public void SendResult(string resultJson)
        {
            Console.WriteLine("===================");
            Console.WriteLine("result:");
            Console.WriteLine(resultJson);
            Console.WriteLine("===================");
        }
    }

 
}
