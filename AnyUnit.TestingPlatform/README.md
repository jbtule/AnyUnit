# AnyUnit.TestingPlatform

[Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
(MTP) adapter for [AnyUnit](https://github.com/jbtule/AnyUnit) - the same
`dotnet test`/`dotnet run`-compatible entry point real NUnit/xUnit's own
MTP mode gives you (`EnableNUnitRunner`/`EnableMSTestRunner`), for an
AnyUnit test project - C# or F# alike.

## Usage

```xml
<PropertyGroup>
  <EnableAnyUnitRunner>true</EnableAnyUnitRunner>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="AnyUnit.TestingPlatform" Version="*" />
</ItemGroup>
```

That's it - a real entry point is generated for you (`OutputType` is
switched to `Exe` automatically too, via Microsoft.Testing.Platform.
MSBuild's own official generator), and the project becomes directly
runnable: `dotnet run`, or `dotnet test` if it's included in your
solution. This works the same way for an `.fsproj` as a `.csproj` - the
generator genuinely emits real F# source there, not just C#, so there's
no F#-specific setup needed.

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

If you'd rather not have a `dotnet test`/MTP-integrated entry point at
all, see [`AnyUnit.Runner.Bootstrap`](../Runner/Bootstrap) instead - a
plain console `Runner.Run(platform)`, its own simpler output format, no
MTP package dependency.

## Other MTP extensions (TRX, and anything else)

`EnableAnyUnitRunner` registers `AnyUnit.TestingPlatform` as a
[`TestingPlatformBuilderHook`](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-extensions-hooks) -
the same generic extensibility point every other MTP extension package
uses - so any of them just work by adding their own `PackageReference`,
with no AnyUnit-specific glue needed. For TRX output specifically:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="*" />
</ItemGroup>
```

```
dotnet run -- --report-trx
```

Same idea for `Microsoft.Testing.Extensions.Retry`,
`Microsoft.Testing.Extensions.CrashDump`, or any future MTP extension -
`AnyUnit.TestingPlatform` itself doesn't need to know about any of them
individually.
