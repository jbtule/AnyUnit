using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.AspNet.SignalR.Client.Hubs;
using PclUnit.Runner;

namespace sl_50_x86_xap
{
    public partial class MainPage : UserControl
    {
        private FakeConsole _fakeConsole ;


        public MainPage(IEnumerable<string> args)
        {
            InitializeComponent();

            _fakeConsole = new FakeConsole(Output);

            Console.WriteLine("start");

            var thread = new Thread(() => RunTests(args));
            thread.Start();
        }


        public class FakeConsole
        {
            private readonly TextBox _output;

            public FakeConsole(TextBox output)
            {
                _output = output;
            }


            public void WriteLine(string p0)
            {

                Deployment.Current.Dispatcher.BeginInvoke(()=>_output.Text+= p0 + "\n");
            }

            
        }

        public FakeConsole Console
        {
            get { return _fakeConsole; }
        }

        private void RunTests(IEnumerable<string> args)
        {
            var id = args.FirstOrDefault();
            var url = args.Skip(1).FirstOrDefault();
            var dlls = args.Skip(2);

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

            var am = dlls.Select(Assembly.Load).ToList();


            Console.WriteLine("Generating Tests...");


            var runner = Generate.Tests(id, am);


            bool run = false;

            serverHub.On<Result>("TestsAreReady", message =>
            {
                Console.WriteLine("Running Tests...");

                runner.RunAll(result => serverHub.Invoke("SendResult",
                                                         result.ToItemJson()).Wait());
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


            Deployment.Current.Dispatcher.BeginInvoke(End);
            
        }


        public void End()
        {
             System.Windows.Browser.HtmlPage.Window.Eval("window.document.title = 'DONE';");
        }

    }
}
