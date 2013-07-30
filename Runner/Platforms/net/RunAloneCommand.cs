using System;
using System.IO;
using System.Linq;
using ManyConsole;

namespace SatelliteRunner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {

        public override int Run(string[] args)
        {
#if NET40
            var id = string.Format("net40-{0}", (Environment.Is64BitProcess ? "x64" : "x86"));
#else
            var id = string.Format("net45-{0}",(Environment.Is64BitProcess ? "x64" : "x86"));
#endif
            Console.WriteLine(id);
            var dlls = args.Select(Path.GetFullPath);
            var results =new RunTests().RunAlone(id, dlls);

            WriteResults.ToFiles(results,_outputs);

            return results.HasError ? 1 : 0;
        }
    }
}
