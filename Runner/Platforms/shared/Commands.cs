using System;
using System.Collections.Generic;
using ManyConsole.CommandLineUtils;

namespace SatelliteRunner.Shared
{
    public partial class RunAloneCommand : ConsoleCommand
    {
        private IDictionary<string,string> _outputs = new Dictionary<string, string>();
        private ConsoleOutputStyle _outputStyle = ConsoleOutputStyle.PlainText;
        private string _platformSuffix;

        public RunAloneCommand()
        {
            IsCommand("run", "runs test runner to output file");
            this.HasOption("o|output=", "Results File Output", v => _outputs.Add(WriteResults.JsonType,v));
            // The Platform this run reports (AnyUnit.Util.PlatformId.Current,
            // e.g. "net10-osx-arm64") is already auto-detected and already
            // distinguishes OS/arch/framework - this is only for a caller who
            // wants to distinguish something PlatformId itself has no way to
            // see, e.g. two CI legs that are otherwise identical framework/
            // OS/arch (a "-nightly" vs "-pr" run, a container/non-container
            // split, ...). Appended, not a replacement - see RunAloneCommand's
            // own Run().
            this.HasOption("p|platform-suffix=", "Optional label appended to the auto-detected platform id (e.g. -p ci-nightly).", v => _platformSuffix = v);
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
