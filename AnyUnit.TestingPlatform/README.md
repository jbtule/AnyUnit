# AnyUnit.TestingPlatform

[Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
(MTP) adapter for [AnyUnit](https://github.com/jbtule/AnyUnit) - the same
`dotnet test`/`dotnet run`-compatible entry point real NUnit/xUnit's own
MTP mode gives you (`EnableNUnitRunner`/`EnableMSTestRunner`), for an
AnyUnit test project - C# or F# alike.

## Usage

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <EnableAnyUnitRunner>true</EnableAnyUnitRunner>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="AnyUnit.TestingPlatform" Version="*" />
</ItemGroup>
```

That's it - a real entry point is generated for you, via Microsoft.
Testing.Platform.MSBuild's own official generator, and the project
becomes directly runnable: `dotnet run`, or `dotnet test` if it's included in your
solution. This works the same way for an `.fsproj` as a `.csproj` - the
generator genuinely emits real F# source there, not just C#, so there's
no F#-specific setup needed.

`OutputType` is set by the project, not by the package, for the same
reason MSTest's `EnableMSTestRunner` asks the same of you: the package
does set it as a fallback, but from its `.targets`, which is after the
SDK has already decided the output's extension - fine for a .NET 10
build (which is a `.dll` either way), but a `net48` build would come
out as `Tests.dll` instead of `Tests.exe`. Found by a real consumer
adding a net48 target.

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

## AnyUnit's own results.json (`--report-anyunit-json`)

```
dotnet run -- --report-anyunit-json
dotnet run -- --report-anyunit-json results.json
```

Writes the same `results.json` the console runners produce with
`-o` - the same schema, from the same serializer, so anything that
reads one reads the other. That includes
[`anyunit-report`](../Report), which converts it to JUnit/TRX/NUnit/
xUnit/CTRF/HTML/Markdown, and merges several runs' files into one.

Unlike `--report-trx`, this needs no extra package - it's built into
`AnyUnit.TestingPlatform` itself, since it's AnyUnit's own format rather
than a general MTP one.

The path is optional. Given one, it's used as-is (relative to the
current directory, like any other `-o`); given none, a timestamped file
is written under `--results-directory`, the way `--report-trx` behaves.
A discovery-only run (`--list-tests`) writes nothing at all: it produces
no results, and an empty results file is something AnyUnit's own tooling
treats as a failure rather than as "nothing ran".

## Telling runs apart (`--platform-suffix`)

```
dotnet run -- --report-anyunit-json --platform-suffix nightly
```

The platform id an MTP run reports is the auto-detected one
(`net10-osx-arm64`), the same as the console runners report.
`--platform-suffix` appends a label to it (`net10-osx-arm64-nightly`) -
the same thing `anyunit-runner run -p` does - for when two runs of the
same assembly on the same OS/arch/framework are genuinely different
things and should not merge into one column of a combined report.
(Before 1.2.1 an MTP run always reported `-mtp` on the end; if you were
relying on that, pass `--platform-suffix mtp`.)

## browser-wasm, without a browser

`EnableAnyUnitRunner` works in a browser-wasm project too (an
`Sdk.BlazorWebAssembly` project with a plain console `Main` - see
`WhoTestsTheTesters/Tests/BasicTests.Wasm.Mtp`). `dotnet.js` boots such
a build directly under `node` or `bun`, and Microsoft.Testing.Platform's
pipeline runs unmodified on Mono-wasm there. One difference from a
desktop run: a results file lands in Emscripten's in-memory filesystem,
so the JS harness copies it out afterwards - that project's
`wwwroot/runtests.mjs` is the whole recipe, a dozen lines.

`dotnet run` runs it: the targets point the project's run command at
that harness under `bun` (`-p:AnyUnitWasmJsHost=node` for node) instead
of the wasm SDK's dev-server-plus-browser, and MTP's arguments pass
through as usual:

```
dotnet run -- --report-anyunit-json results.json
```

`dotnet test` does **not**, and cannot: in MTP mode it talks to the
test app over a named pipe, and the IDE test explorers over TCP -
neither exists on the wasm runtime, so both die before discovery. The
targets mark a wasm test project `IsTestProject=false` so a
solution-wide `dotnet test` skips it rather than failing on it. Known
rough edges: `--list-tests` prints the summary rather than the names,
and an error path that prints usage (an unknown option, say) crashes
looking up the process's own path.

**F# on wasm: one extra line in the harness.** An `.fsproj` cannot host
the entry point Microsoft.Testing.Platform.MSBuild generates on wasm:
the F# template ends in `Async.RunSynchronously`, which on the
single-threaded runtime blocks the only thread the first time MTP
suspends (banner, then nothing, forever), and F# has no async `Main`
the runtime could await instead. So for an F# project targeting
browser-wasm the targets skip that entry point, keep the project a
library, and the entry point comes from `AnyUnit.TestingPlatform.WasmEntry`
- a tiny C# assembly shipped in this package - which the harness names
as the main assembly:

```js
const instance = await dotnet
    .withMainAssembly('AnyUnit.TestingPlatform.WasmEntry')
    .withApplicationArguments(...args)
    .create();
```

Nothing else changes: `EnableAnyUnitRunner=true`, the same
`--report-anyunit-json`, the same copy-out of the results file. The
line is harmless on a C# project too. `WhoTestsTheTesters/Tests/Style/
FsUnitTests.Wasm.Mtp` is the in-repo example and CI runs it.

## Native AOT

An `EnableAnyUnitRunner` project publishes as a Native AOT executable
the ordinary way:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>
```

```
dotnet publish -r linux-x64
./bin/Release/net10.0/linux-x64/publish/MyTests --report-anyunit-json results.json
```

`EnableAnyUnitRunner` also adds the test assembly (the project itself,
or each `AnyUnitTestAssembly`) as a `TrimmerRootAssembly`: AnyUnit finds
fixtures by reflection, nothing references a test class statically, and
an unrooted publish trims them all and discovers zero tests. A plain
build ignores the item.

AnyUnit's own reflection is trim-clean on that basis (each site is
suppressed with that justification - see `AnyUnit/Util/
TrimmerAttributes.cs`), with one exception: the F# `Async<'T>` bridge
(`AsyncTestResult.FSharpAsyncToTask`) still uses `MakeGenericMethod`,
so ILC reports IL2026/IL2060/IL2075/IL3050 there. With
`TreatWarningsAsErrors` on, `-p:IlcTreatWarningsAsErrors=false` keeps
those as warnings without touching the C# compiler's. An F# `Async<'T>`
test with a value-type `'T` may not have its instantiation available
under AOT; C# `Task`/`Task<T>` tests are fine. CI runs
`WhoTestsTheTesters/Tests/BasicTests.Mtp` this way on every build
(`test-aot-mtp`), and the whole suite comes out right, async tests
included.

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
