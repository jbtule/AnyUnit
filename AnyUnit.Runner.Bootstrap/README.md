# AnyUnit.Runner.Bootstrap

The smallest way to get a real, runnable entry point for
[AnyUnit](https://github.com/jbtule/AnyUnit) tests: a single static
`Runner.Run(platform)` your own `Main` calls directly, in place of a
generated (MTP) or hand-rolled one.

Functionally like [`AnyUnit.Runner`](../Runner/Platforms/net10)
(`anyunit-runner`), but deliberately narrower: no CLI, no argument
parsing, and no "point this at an arbitrary list of external .dll paths"
support - it only ever discovers and runs tests in the calling assembly
(or, via the named-assemblies overload, a caller-specified list of
already-loaded ones).

## When to use this instead of `AnyUnit.TestingPlatform`

`AnyUnit.TestingPlatform` generates a `dotnet test`/`dotnet run`-compatible
entry point for you automatically (C# only, via `EnableAnyUnitRunner`).
Reach for `AnyUnit.Runner.Bootstrap` instead when you need to write that
entry point yourself - an F# test project (F# has no equivalent
source-generator opt-in), or a browser-wasm host with no CLI concept at
all (a plain console `Main`, run under `node`/`bun`).

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

Prints the same human-readable (or, with `teamCity: true`, TeamCity
service-message) output `anyunit-runner`'s own console output uses, and
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
