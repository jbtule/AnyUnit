using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ManyConsole;
using sl_runner;

namespace Runner.Shared
{

    public partial class RunAloneCommand : ConsoleCommand
    {

        public override int Run(string[] remainingArguments)
        {
            var id = string.Format("sl50-{0}", (Environment.Is64BitProcess ? "x64" :"x86"));
            var dlls = remainingArguments.Select(Path.GetFullPath);

            var fullsetOfDlls = Util.GenerateFullListOfDlls(dlls);
            var shared = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) ;
           
            Directory.CreateDirectory(shared);

            var tempXap = Util.CompileNewXAP(fullsetOfDlls, shared);

            var htmlname = Util.CreateNewHtml(shared, tempXap, null, id, dlls);



            var thread = Util.LaunchBrowserRunner(true, Path.Combine(shared, htmlname), _outputs, cleanupdir:shared);

            //If silverlight doesn't start in 2 minutes
            //kill self
            Thread.Sleep(new TimeSpan(0, 0, 2, 0));
            if (!Util.started)
            {
                Application.Exit();
            }

            thread.Join();
            
            return 0;
        }
    }
}
