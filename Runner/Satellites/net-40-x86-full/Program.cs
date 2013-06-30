using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Client.Hubs;
using PclUnit.Runner;

namespace net_runner
{
    class Program
    {
        private static IEnumerable<Test> Tests;

        static void Main(string[] args)
        {
            var url = args.First();
            var dlls = args.Skip(1);

            Console.WriteLine(url);
            Console.WriteLine(dlls.First());
            var hubConnection = new HubConnection(url);
            var serverHub = hubConnection.CreateHubProxy("PclUnitHub");

            hubConnection.Start().Wait();


            var pm = new PlatformMeta()
            {
           
                Arch = Environment.Is64BitProcess ? "x64" : "x86",
#if NET40
                Name = "net",
                Version = "40",
#elif NET45
                Name = "net",
                Version = "45",  
#endif
                Profile = "full"
            };

            serverHub.Invoke("Connect", pm.UniqueName).Wait();
              
          

            var am = dlls.Select(it=>Assembly.LoadFile(it)).ToList();




           var tests = Generate.Tests(pm, am);

            bool run = false;
            
            serverHub.On("TestsAreReady", message =>
                                              {
                                                  foreach (var test in tests)
                                                  {
                                                      var result = test.Run();
                                                      serverHub.Invoke("SendResult", result.ToItemJson()).Wait();
                                                  }
                                                  run = true;
                                              });

            serverHub.Invoke("List", pm.ToListJson()).Wait();

           SpinWait.SpinUntil(()=> { return run; });
        }
    }
}
