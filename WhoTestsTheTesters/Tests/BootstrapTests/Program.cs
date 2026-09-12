using System.IO;

// The whole point of AnyUnit.Runner.Bootstrap: this is a real Main, not a
// generated one, calling Runner.Run directly - Assembly.GetCallingAssembly()
// inside Run() resolves to THIS assembly (BootstrapTests.dll), discovering
// and running the [Test]s in Basic.cs.
//
// First arg (optional), if given, is a JSON output path - same shape
// run-tests.sh's own `-o` convention uses for anyunit-runner/
// anyunit-browser-wasm, so this self-test can be wired into the same
// ConventionTestProcessor verification the rest of the repo's self-tests
// already go through. Run() itself takes a Stream, not a path (a platform
// without a meaningful file system still has to be able to call it) - this
// entry point is the one that actually has a real file system, so it opens
// the file itself.
internal static class Program
{
    private static int Main(string[] args)
    {
        var jsonOutputPath = args.Length > 0 ? args[0] : null;
        using (var jsonOutputStream = jsonOutputPath != null ? File.Create(jsonOutputPath) : null)
        {
            return AnyUnit.Runner.Bootstrap.Runner.Run("net10", jsonOutputStream: jsonOutputStream);
        }
    }
}
