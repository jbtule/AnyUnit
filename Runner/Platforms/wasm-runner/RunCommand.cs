using ManyConsole.CommandLineUtils;

namespace WasmRunner;

public class RunCommand : ConsoleCommand
{
    private readonly IDictionary<string, string> _outputs = new Dictionary<string, string>();
    private bool _teamCity;

    public RunCommand()
    {
        // Same flag shape as net10-runner/net48-runner's RunAloneCommand
        // (Runner/Platforms/shared) - this is meant to be a drop-in sibling,
        // not a differently-shaped tool.
        IsCommand("run", "runs test runner to output file");
        this.HasOption("o|output=", "Results File Output", v => _outputs.Add("json", v));
        this.HasOption("teamcity", "Team City results to Std out.", v => { _teamCity = true; });
        HasAdditionalArguments(null, " <assemblypaths...>");
    }

    public override int Run(string[] args)
    {
        var dlls = args.Select(Path.GetFullPath).ToList();
        var hasError = WasmRunAlone.RunAsync(dlls, _outputs, _teamCity).GetAwaiter().GetResult();
        return hasError ? 1 : 0;
    }
}
