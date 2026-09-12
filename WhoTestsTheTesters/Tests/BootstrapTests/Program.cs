// The whole point of AnyUnit.Runner.Bootstrap: this is a real Main, not a
// generated one, calling Runner.Run directly - Assembly.GetCallingAssembly()
// inside Run() resolves to THIS assembly (BootstrapTests.dll), discovering
// and running the [Test]s in Basic.cs.
//
// First arg (optional), if given, is a JSON output path - same shape
// run-tests.sh's own `-o` convention uses for anyunit-runner/
// anyunit-browser-wasm, so this self-test can be wired into the same
// ConventionTestProcessor verification the rest of the repo's self-tests
// already go through.
internal static class Program
{
    private static int Main(string[] args)
    {
        var jsonOutputPath = args.Length > 0 ? args[0] : null;
        return AnyUnit.Runner.Bootstrap.Runner.Run("net10", jsonOutputPath: jsonOutputPath);
    }
}
