using ManyConsole.CommandLineUtils;

namespace BrowserWasmRunner;

public class RunCommand : ConsoleCommand
{
    private readonly IDictionary<string, string> _outputs = new Dictionary<string, string>();
    private bool _teamCity;
    private bool _noSandbox;
    private string? _platformSuffix;

    public RunCommand()
    {
        // Same flag shape as net10-runner/net48-runner's RunAloneCommand
        // (Runner/Platforms/shared) - this is meant to be a drop-in sibling,
        // not a differently-shaped tool.
        IsCommand("run", "runs test runner to output file");
        this.HasOption("o|output=", "Results File Output", v => _outputs.Add("json", v));
        this.HasOption("teamcity", "Team City results to Std out.", v => { _teamCity = true; });
        this.HasOption("p|platform-suffix=", "Optional label appended to the auto-detected platform id (e.g. -p ci-nightly).", v => _platformSuffix = v);
        // Already auto-detected on Linux CI (see WasmRunAlone's own
        // comment - Chromium's sandbox commonly fails there under
        // AppArmor restrictions on unprivileged user namespaces). This
        // is an explicit override for when that heuristic guesses wrong
        // - e.g. a self-hosted or Docker environment the CI env var
        // doesn't happen to be set in.
        this.HasOption("no-sandbox", "Force Chromium's --no-sandbox on, overriding the Linux CI auto-detection.", v => { _noSandbox = true; });
        HasAdditionalArguments(null, " <assemblypaths...>");
    }

    public override int Run(string[] args)
    {
        var dlls = args.Select(Path.GetFullPath).ToList();
        var hasError = WasmRunAlone.RunAsync(dlls, _outputs, _teamCity, _noSandbox, _platformSuffix).GetAwaiter().GetResult();
        return hasError ? 1 : 0;
    }
}
