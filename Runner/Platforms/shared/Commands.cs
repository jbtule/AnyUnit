using System.Collections.Generic;
using ManyConsole.CommandLineUtils;

namespace SatelliteRunner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {
        private IDictionary<string,string> _outputs = new Dictionary<string, string>();
        private bool _teamCity;

        public RunAloneCommand()
        {
            IsCommand("run", "runs test runner to output file");
            this.HasOption("o|output=", "Results File Output", v => _outputs.Add(WriteResults.JsonType,v));
            this.HasOption("teamcity", "Team City results to Std out.", v => { _teamCity = true; });
            HasAdditionalArguments(null, " <assemblypaths...>");
        }
    }
}
