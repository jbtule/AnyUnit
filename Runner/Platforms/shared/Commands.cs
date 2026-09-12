using System;
using System.Collections.Generic;
using ManyConsole.CommandLineUtils;

namespace SatelliteRunner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {
        private IDictionary<string,string> _outputs = new Dictionary<string, string>();
        private ConsoleOutputStyle _outputStyle = ConsoleOutputStyle.PlainText;

        public RunAloneCommand()
        {
            IsCommand("run", "runs test runner to output file");
            this.HasOption("o|output=", "Results File Output", v => _outputs.Add(WriteResults.JsonType,v));
            // Names the option after the enum it sets, and takes the enum's
            // own member name as its value (case-insensitive) - so the CLI
            // surface and ConsoleOutputStyle stay one thing to keep in sync,
            // not two (a flag name plus a separate enum value it happens to
            // mean). -teamcity below is kept only as a shorthand alias for
            // -output-style=TeamCity, for scripts already written against it.
            this.HasOption("s|output-style=", "Console output style: PlainText (default) or TeamCity.", v =>
            {
                ConsoleOutputStyle style;
                if (!Enum.TryParse(v, ignoreCase: true, out style))
                {
                    throw new ConsoleHelpAsException(string.Format(
                        "Unknown -output-style '{0}' - expected PlainText or TeamCity.", v));
                }
                _outputStyle = style;
            });
            this.HasOption("teamcity", "Shorthand for -output-style=TeamCity.", v => { _outputStyle = ConsoleOutputStyle.TeamCity; });
            HasAdditionalArguments(null, " <assemblypaths...>");
        }
    }
}
