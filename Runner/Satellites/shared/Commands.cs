using System.Collections.Generic;
using ManyConsole;

namespace Runner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {
        private IDictionary<string,string> _outputs = new Dictionary<string, string>();

        public RunAloneCommand()
        {
            IsCommand("run", "runs test runner to output file");
            this.HasOption("o|output=", "Results File Output", v => _outputs.Add(WriteResults.JsonType,v));
            this.HasOption("nunit-output=", "Results File Output in Nunit XML", v => _outputs.Add(WriteResults.NunitType, v));
            HasAdditionalArguments(null, " <assemblypaths...>");
        }
    }

    public partial class RunSatelliteCommand : ConsoleCommand
    {
        public RunSatelliteCommand()
        {
            IsCommand("sat", "runs tests runner as a satellite");
            HasAdditionalArguments(null, " <hidden|show> <id> <url> <tempdir> <assemblypaths...>");
        }

    }
}