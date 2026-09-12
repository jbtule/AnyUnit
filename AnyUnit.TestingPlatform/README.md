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

That's it - a `Program.cs`/`Program.fs` with a real `Main` is generated
for you (`OutputType` is switched to `Exe` automatically too), and the
project becomes directly runnable: `dotnet run`, or `dotnet test` if it's
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

## C# only

The generated entry point is plain C#, and this package's own code-
generation target only knows how to inject a `.cs` file into the build -
an F# project needs its own hand-written entry point instead. See
[`AnyUnit.Runner.Bootstrap`](../Runner/Bootstrap) for the smallest
way to write one (`AnyUnit.Runner.Bootstrap.Runner.Run(platform)`), which
also works from C# if you'd rather not generate a `Main` at all.
