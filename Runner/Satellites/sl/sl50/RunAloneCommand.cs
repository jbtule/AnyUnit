using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ManyConsole;
using PclUnit.Runner;
using sl_runner;

namespace Runner.Shared
{

    public partial class RunAloneCommand : ConsoleCommand
    {

        private string _shared;
        private static string _id;
        public override int Run(string[] remainingArguments)
        {
            _id = string.Format("sl50-{0}", (Environment.Is64BitProcess ? "x64" :"x86"));
            Console.WriteLine(_id);
            Console.WriteLine("Silverlight tests have to wait until completion when run alone.");
            var dlls = remainingArguments.Select(Path.GetFullPath);

            var fullsetOfDlls = Util.GenerateFullListOfDlls(dlls);
            _shared = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            Directory.CreateDirectory(_shared);

            var tempXap = Util.CompileNewXAP(fullsetOfDlls, _shared);

            var htmlname = Util.CreateNewHtml(_shared, tempXap, null, _id, dlls);



            var thread = Util.LaunchBrowserRunner(true, Path.Combine(_shared, htmlname), WhenFinished, WhenExiting);

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

        public void WhenExiting()
        {
            Console.WriteLine("Cleaning Up");
            if (!String.IsNullOrWhiteSpace(_shared))
            {
                try
                {
                    //Try Delete Temp shared path
                    Directory.Delete(_shared);
                }
                catch { }
            }
        }


        public void WhenFinished(ResultsFile file)
        {

            var rt = new RunTests();

            rt.PrintOutAloneStart(_id);
            foreach (var r in file.Results)
            {
                rt.PrintOutAloneResults(r);
            }
            rt.PrintOutAloneEnd(_id, file);

            if (_outputs != null && _outputs.Any())
            {
                WriteResults.ToFiles(file, _outputs);
                Environment.ExitCode = file.HasError ? 1 : 0;
            }
        }
    }
}
