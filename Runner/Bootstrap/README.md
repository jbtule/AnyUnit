# AnyUnit.Runner.Bootstrap

The smallest way to get a real, runnable entry point for
[AnyUnit](https://github.com/jbtule/AnyUnit) tests: a single static
`Runner.Run(platform)` your own `Main` calls directly, in place of a
generated (MTP) or hand-rolled one.

Functionally like [`AnyUnit.Runner`](../Platforms/net10)
(`anyunit-runner`), but deliberately narrower: no CLI, no argument
parsing, and no "point this at an arbitrary list of external .dll paths"
support - it only ever discovers and runs tests in the calling assembly
(or, via the named-assemblies overload, a caller-specified list of
already-loaded ones).

## When to use this instead of `AnyUnit.TestingPlatform`

`AnyUnit.TestingPlatform` generates a `dotnet test`/`dotnet run`-compatible
entry point for you automatically, but only for C# (its code-generation
target only knows how to inject a `.cs` file). An F# project can still
get a real MTP-integrated entry point by calling
`AnyUnit.TestingPlatform.Runner.RunAsync` directly from its own
hand-written `Program.fs` - see that package's own README.

Reach for `AnyUnit.Runner.Bootstrap` instead when you don't want an MTP-
integrated entry point at all - its own simpler output format, no MTP
package dependency, and no headless-browser/dev-server concept to work
around for a browser-wasm host (a plain console `Main`, run under
`node`/`bun`).

## Usage

```csharp
internal static class Program
{
    private static int Main(string[] args)
    {
        var jsonOutputPath = args.Length > 0 ? args[0] : null;
        return AnyUnit.Runner.Bootstrap.Runner.Run("net10", jsonOutputPath: jsonOutputPath);
    }
}
```

```fsharp
[<EntryPoint>]
let main _ = AnyUnit.Runner.Bootstrap.Runner.Run("net10")
```

Prints the same human-readable (or, with `outputStyle: ConsoleOutputStyle.TeamCity`,
TeamCity service-message) output `anyunit-runner`'s own console output uses, and
returns a process exit code (0 if every test passed).

To discover tests across more than one already-loaded assembly instead of
just the calling one (e.g. a browser-wasm host that statically links
several test assemblies together):

```csharp
AnyUnit.Runner.Bootstrap.Runner.Run("browser-wasm", new[] { "MyTests", "MyOtherTests" });
```

Each name is matched by simple assembly name against whatever's already
loaded into the process, falling back to `Assembly.Load(name)` if it
isn't preloaded.
