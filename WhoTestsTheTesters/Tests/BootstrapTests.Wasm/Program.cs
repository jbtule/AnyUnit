// Plain console entry point - no WebAssemblyHostBuilder, no Razor
// components, no UI. dotnet.js detects a non-browser JS host (node/bun)
// and boots directly without one - see
// Runner/Platforms/browser-wasm-runner-host/Readme.md's own "no headless
// browser needed" section for the full story. Assembly.GetCallingAssembly()
// inside Runner.Run resolves to THIS assembly (BootstrapTests.Wasm.dll),
// discovering and running the same [Test]s in Basic.cs (compiled directly
// into this assembly) that the desktop BootstrapTests project runs too.
internal static class Program
{
    private static int Main(string[] args)
    {
        var jsonOutputPath = args.Length > 0 ? args[0] : null;
        return AnyUnit.Runner.Bootstrap.Runner.Run("browser-wasm", jsonOutputPath: jsonOutputPath);
    }
}
