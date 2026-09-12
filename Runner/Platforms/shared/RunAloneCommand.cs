using System;
using System.IO;
using System.Linq;
using ManyConsole.CommandLineUtils;

namespace SatelliteRunner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {
#if NET48
        private const string RunnerId = "net48";
#else
        private const string RunnerId = "net10";
#endif

        public override int Run(string[] args)
        {
            Console.WriteLine(RunnerId);
            var dlls = args.Select(Path.GetFullPath);
            var results = new RunTests { TeamCity = _teamCity }.RunAlone(RunnerId, dlls);

            WriteResults.ToFiles(results, _outputs);

            return results.HasError ? 1 : 0;
        }
    }
}
