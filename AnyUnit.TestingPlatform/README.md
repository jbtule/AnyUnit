# AnyUnit.TestingPlatform

[Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
(MTP) adapter for [AnyUnit](https://github.com/jbtule/AnyUnit) - the same
`dotnet test`/`dotnet run`-compatible entry point real NUnit/xUnit's own
MTP mode gives you (`EnableNUnitRunner`/`EnableMSTestRunner`), for an
AnyUnit test project.

## Usage

```xml
<PropertyGroup>
  <EnableAnyUnitRunner>true</EnableAnyUnitRunner>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="AnyUnit.TestingPlatform" Version="*" />
</ItemGroup>
```

That's it - a `Program.cs` with a real `Main` is generated for you
(`OutputType` is switched to `Exe` automatically too), and the project
becomes directly runnable: `dotnet run`, or `dotnet test` if it's
included in your solution.

By default the generated entry point tests the project's own entry
assembly - the normal case, when `EnableAnyUnitRunner` is set directly on
the project that *is* the test assembly. A satellite runner project that
only `ProjectReference`s the real test assembly instead lists it
explicitly, by simple assembly name:

```xml
<ItemGroup>
  <AnyUnitTestAssembly Include="MyRealTestAssembly" />
</ItemGroup>
```

## F# (or any hand-written entry point)

`EnableAnyUnitRunner`'s code-generation only knows how to inject a `.cs`
file into the build, so it's C#-only - an F# project needs its own
hand-written entry point instead. The generated C# `Main` is just one
line calling `AnyUnit.TestingPlatform.Runner.RunAsync` - a real, compiled
method, not generated text - so an F# project can call the exact same
thing directly, for a real MTP/`dotnet test`-integrated `Program.fs`:

```fsharp
[<EntryPoint>]
let main args =
    AnyUnit.TestingPlatform.Runner.RunAsync(args).GetAwaiter().GetResult()
```

Pass assemblies explicitly (same meaning as `<AnyUnitTestAssembly>`
above) for a satellite project: `Runner.RunAsync(args, myAssembly)`.

If you'd rather not have a `dotnet test`/MTP-integrated entry point at
all, see [`AnyUnit.Runner.Bootstrap`](../Runner/Bootstrap) instead - a
plain console `Runner.Run(platform)`, its own simpler output format, no
MTP package dependency.

## TRX output

Add a reference to `Microsoft.Testing.Extensions.TrxReport` (the real
MTP-ecosystem TRX generator - the same one `EnableMSTestRunner`/
`EnableNUnitRunner`/`EnableXUnitRunner` projects use) to get a real
`--report-trx` flag:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="*" />
</ItemGroup>
```

```
dotnet run -- --report-trx
```

`AnyUnit.TestingPlatform` itself only references the small
`Microsoft.Testing.Extensions.TrxReport.Abstractions` package (the
capability/property types a test framework implements against, not the
generator) - it picks up the real generator via reflection at runtime,
only once your project references it, so a project that doesn't want
TRX output never pays for the larger package.
