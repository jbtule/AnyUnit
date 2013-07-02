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
        static void Main(string[] args)
        {
            var id = args.First();
            var url = args.Skip(1).First();
            var dlls = args.Skip(2);

#if x64
            if(!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif


            Console.WriteLine(url);
            Console.WriteLine(dlls.First());
            var hubConnection = new HubConnection(url);
            var serverHub = hubConnection.CreateHubProxy("PclUnitHub");

            hubConnection.Start().Wait();


            var pm = new PlatformMeta(id);

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
