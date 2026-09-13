using System;
using System.IO;
using System.Linq;
using AnyUnit.Util;
using ManyConsole.CommandLineUtils;

namespace SatelliteRunner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {
        public override int Run(string[] args)
        {
            var runnerId = PlatformId.Current;
            if (!string.IsNullOrEmpty(_platformSuffix))
                runnerId = runnerId + "-" + _platformSuffix;

            Console.WriteLine(runnerId);
            var dlls = args.Select(Path.GetFullPath);
            var results = new RunTests { OutputStyle = _outputStyle }.RunAlone(runnerId, dlls);

            WriteResults.ToFiles(results, _outputs);

            return results.HasError ? 1 : 0;
        }
    }
}
