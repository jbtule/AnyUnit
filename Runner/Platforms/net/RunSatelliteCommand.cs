using System.Linq;
using ManyConsole;

namespace SatelliteRunner.Shared
{

    public partial class RunSatelliteCommand : ConsoleCommand
    {

        public override int Run(string[] args)
        {
            var hidden = args.First();
            var id = args.Skip(1).First();
            var url = args.Skip(2).First();
            var sharedpath = args.Skip(3).First();
            var dlls = args.Skip(4);
            new RunTests().Run(id, url, dlls.ToArray());
            return 0;
        }
    }
}