using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using PclUnit.Runner;

namespace net_runner
{
    class Program
    {
        static Program()
        {
        }

     
        static void Main(string[] args)
        {

            var id = args.First();
            var url = args.Skip(1).First();
            var sharedpath = args.Skip(2).First();
            var dlls = args.Skip(3);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;


#if x64
            if(!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif

            Console.WriteLine("id:" + id);
            Console.WriteLine("server:"+url);
            Console.WriteLine("tests dlls:");
            foreach (var dll in dlls)
            {
                Console.WriteLine(dll);
            }
            Console.WriteLine("==========================");

            var hubConnection = new HubConnection(url);
            var serverHub = hubConnection.CreateHubProxy("PclUnitHub");

            hubConnection.Start().Wait();


            serverHub.Invoke("Connect", id).Wait();

            var am = dlls.Select(Assembly.LoadFile).ToList();




            var runner = Generate.Tests(id, am);

            bool run = false;
            
            serverHub.On("TestsAreReady", message =>
                                              {
                                                  runner.RunAll(result => serverHub.Invoke("SendResult",
                                                                                           result.ToItemJson()).Wait());
                                                  run = true;
                                              });

            serverHub.Invoke("List", runner.ToListJson()).Wait();

            while (true)
            {
                Thread.Sleep(50);
                if(run)
                    break;
            }
        }



        private static Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var baseUri = new Uri(args.RequestingAssembly.CodeBase); 
            var shortName = new AssemblyName(args.Name).Name;

            Console.WriteLine(args.RequestingAssembly.CodeBase);
            Console.WriteLine(shortName);
            string fullName = Path.Combine(Path.GetDirectoryName(Uri.UnescapeDataString(baseUri.AbsolutePath)), string.Format("{0}.dll", shortName));
            Console.WriteLine(fullName);
            Assembly asm = null;
            try
            {

                asm = Assembly.LoadFile(fullName);
            }
            catch(Exception ex)
            {
                
                Console.WriteLine(ex);
            }
            return asm;
        }
    }
}
