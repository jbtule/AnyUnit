using System.IO;

// Plain console entry point - no WebAssemblyHostBuilder, no Razor
// components, no UI. dotnet.js detects a non-browser JS host (node/bun)
// and boots directly without one - see
// Runner/Platforms/browser-wasm-runner-host/Readme.md's own "no headless
// browser needed" section for the full story. Assembly.GetCallingAssembly()
// inside Runner.Run resolves to THIS assembly (BootstrapTests.Wasm.dll),
// discovering and running the same [Test]s in Basic.cs (compiled directly
// into this assembly) that the desktop BootstrapTests project runs too.
//
// Run() takes a Stream for its JSON output, not a path (a platform with no
// meaningful file system still has to be able to call it) - browser-wasm's
// own virtual file system does support plain File.Create fine, so this
// entry point still opens one from the given path.
internal static class Program
{
    private static int Main(string[] args)
    {
        var jsonOutputPath = args.Length > 0 ? args[0] : null;
        using (var jsonOutputStream = jsonOutputPath != null ? File.Create(jsonOutputPath) : null)
        {
            return AnyUnit.Runner.Bootstrap.Runner.Run("browser-wasm", jsonOutputStream: jsonOutputStream);
        }
    }
}
