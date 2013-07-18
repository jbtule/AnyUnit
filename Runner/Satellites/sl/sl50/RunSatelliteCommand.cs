using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ManyConsole;
using sl_runner;

namespace Runner.Shared
{
    public partial class RunSatelliteCommand : ConsoleCommand
    {
     
        public override int Run(string[] args)
        {
            var hidden = args.First();
            var id = args.Skip(1).First();
            var url = args.Skip(2).First();
            var shared = args.Skip(3).First();
            var dlls = args.Skip(4);

            var ishidden = hidden.Contains("hidden");
            var fullsetOfDlls = Util.GenerateFullListOfDlls(dlls);

            var tempPath = Util.CompileNewXAP(fullsetOfDlls, shared);
            Console.WriteLine(tempPath);

            var htmlname = Util.CreateNewHtml(shared, tempPath, url, id, dlls);

            var temphtml = string.Format("{0}/reshare/{1}", url, htmlname);

            Console.WriteLine(temphtml);

            var thread = Util.LaunchBrowserRunner(ishidden, temphtml);

            //If silverlight doesn't start in 2 minutes
            //kill self
            Thread.Sleep(new TimeSpan(0, 0, 2, 0));
            if (!Util.started)
            {
                Application.Exit();
            }

            return 0;
        }

    }
}
