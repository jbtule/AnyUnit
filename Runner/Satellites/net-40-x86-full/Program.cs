using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace net_40_x86_full
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = args.First();
            var dlls = args.Skip(1);


            var hubConnection = new HubConnection(url);
            var serverHub = hubConnection.CreateHubProxy("PclUnitHub");
            serverHub.On("broadCastToClients", message => System.Console.WriteLine(message));
            hubConnection.Start().Wait();
            string line = null;
            while ((line = System.Console.ReadLine()) != null)
            {
                // Send a message to the server
                serverHub.Invoke("Connect", "net-40-x86-full").Wait();
            }

            System.Console.Read();
        }
    }
}
