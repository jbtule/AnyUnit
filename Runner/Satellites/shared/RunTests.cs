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
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using PclUnit.Runner;

namespace Runner.Shared
{
    public partial class RunTests
    {
       
        public ResultsFile RunAlone(string id, IEnumerable<string> dlls)
        {

#if x64
            if(!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif

#if SILVERLIGHT
            var am = dlls.Select(Assembly.Load).ToList();
#else
            var am = dlls.Select(Assembly.LoadFile).ToList();
#endif

            var runner = Generate.Tests(id, am);
            PrintOutAloneStart(id);
            var file = new ResultsFile();
            runner.RunAll(r =>
                              {
                                  PrintOutAloneResults(r);
                                  file.Add(r);
                              });
            PrintOutAloneEnd(id,file);
            return file;
        }

 	


        public void Run(string id, string url, string[] dlls)
        {

#if x64
            if(!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif

            Console.WriteLine("id:" + id);
            Console.WriteLine("server:" + url);
            Console.WriteLine("tests dlls:");
            foreach (var dll in dlls)
            {
                Console.WriteLine(dll);
            }
            Console.WriteLine("==========================");

            var hubConnection = new HubConnection(url);
            var serverHub = hubConnection.CreateHubProxy("PclUnitHub");
            Console.WriteLine("waiting..");
            hubConnection.Start().Wait();
            Console.WriteLine("Connecting..");

            serverHub.Invoke("Connect", id).Wait();

            Console.WriteLine("Loading dlls..");

#if SILVERLIGHT
            var am = dlls.Select(Assembly.Load).ToList();
#else
            var am = dlls.Select(Assembly.LoadFile).ToList();
#endif

            Console.WriteLine("Generating Tests...");


            var runner = Generate.Tests(id, am);


            bool run = false;

            serverHub.On<string[]>("TestsAreReady", includes =>
                                                      {
                                                          Console.WriteLine("Running Tests...");

                                                          runner.RunAll(result => serverHub.Invoke("SendResult",
                                                                                                   result.ToItemJson()).Wait(),
                                                                                                 TestFilter.Create(includes)
                                                                                                   );
                                                          run = true;
                                                      });

            Console.WriteLine("Sending Tests...");

            serverHub.Invoke("List", runner.ToListJson()).Wait();

            while (true)
            {
                Thread.Sleep(50);
                if (run)
                    break;
            }

            Console.WriteLine("Quiting...");



        }

        
    }
   
}