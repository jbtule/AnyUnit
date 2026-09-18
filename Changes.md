# Changes

A high-level catalog of the eras this repo has gone through, not a
per-commit changelog - each one was a real, working target at the time,
not an abandoned experiment.

Since 1.0 there are also per-release notes below the era list, for the
ordinary "what changed in this version" question the eras don't answer.

## Releases

### 1.2.3 - in development

- `dotnet run` on a browser-wasm test project runs the tests under bun
  (or node) through its JS harness instead of launching the wasm SDK's
  dev server and a browser. `dotnet test` cannot follow (it needs a
  named pipe the wasm runtime lacks); such projects are now
  `IsTestProject=false` so a solution-wide `dotnet test` skips them.
- `TestCapabilities.Threads`: for a test body that blocks waiting on
  work running on another thread (`Task.Run(...).Wait()`, `.Result`,
  F#'s `Async.RunSynchronously` under a synchronization context).
  Absent on single-threaded browser-wasm, where such a wait hangs the
  run and nothing can interrupt it; declared, the test is reported
  Ignored there.

- Native AOT: an `EnableAnyUnitRunner` project publishes and runs with
  `PublishAot=true`. The targets root the test assembly for the trimmer
  (reflection-discovered fixtures were otherwise trimmed to zero tests),
  and `Task<T>.Result` is kept for async tests (its getter was trimmed,
  so every async test errored), and every other reflection site is
  suppressed on the strength of that rooting. An F# `[<Test>]` body
  that is an `async { ... }` works too: `EnableAnyUnitRunner` hands ILC
  an rd.xml for the `Async<unit>`/`Async<bool>` instantiations whenever
  FSharp.Core is referenced (a generic instantiation reflection asks
  for at run time has to have been compiled in), and any `Async<'T>` the
  engine cannot start is now a clear Error rather than a test that
  passes without its body ever being awaited. `AnyUnit.Constraints` and
  the NUnit/xUnit/MSTest/FsUnit styles are trim-clean too, with the AOT
  bugs each hid fixed: `Has.Length`/`Count`/`Message`/`InnerException`
  on BCL values (their properties were trimmed), xUnit's `IUseFixture<T>`
  (the interface was trimmed off its implementing class, so `SetFixture`
  never ran), MSTest's `TestContext` (never injected). CI publishes and
  runs every C#-hosted `*.Mtp` self-test project this way on every
  build.
- `TestCapabilities.FrameworkReflection`: for a test that reflects by
  name over a *framework* type it does not own. Absent under Native AOT,
  where an unused framework member is trimmed and reflection truthfully
  reports it missing; declared, the test is reported Ignored there.

- `[assembly: TestFixtureDiscovery(TargetOfGenerator, StaticMethodOfGenerator)]`
  works: it looked the generator method up with a `Type` parameter while
  calling it with the `Assembly` the `FixtureGenerator` delegate takes,
  so no real generator was ever found and discovery threw. Found by
  BasicTests growing a suite of engine-path tests (custom discovery, a
  throwing Dispose/one-time setup/SetUpFixture, a returned
  `IReturnedResult`, a per-row `IgnoreReason`, `TestFilter`, and log
  output exercising every JSON escape), which take the core's line
  coverage from 78% to 88%.

### 1.2.2 - 2026-09-16

- F# test projects work on browser-wasm through MTP under node/bun. The
  entry point Microsoft.Testing.Platform.MSBuild generates for an
  `.fsproj` blocks in `Async.RunSynchronously` on the single-threaded
  runtime; `AnyUnit.TestingPlatform` now ships
  `AnyUnit.TestingPlatform.WasmEntry`, an awaitable entry point the JS
  harness names with `withMainAssembly`, and its targets skip the
  generated one for F#-on-wasm. `EnableAnyUnitRunner=true` is still all
  the project says.
- The browser-wasm runner's README lists what hangs a run there
  (`Async.RunSynchronously` regardless of the workflow, blocking waits)
  and how to find the test.

### 1.2.1 - 2026-09-16

- `AnyUnit.Run.AmbientTest.Current` (and `Assert.Current`): the running
  test's assertion helper, set by the engine around every test body, for
  code with no `this` to assert through. FsUnit's free-function `should`/
  `shouldFail` now use it - counted against the test, `NoError` intact -
  and are no longer `[<Obsolete>]`; Expecto's `Expect` moves onto the
  same slot.

- `AnyUnit.TestingPlatform` reports the plain platform id (`net10-linux-x64`),
  the same as the console runners, instead of always appending `-mtp`.
  That suffix only ever served AnyUnit's own CI, which runs the same
  assemblies both ways; it now passes `--platform-suffix mtp` itself.
  Pass that yourself if you were relying on the old label.
- `EnableAnyUnitRunner` works in a browser-wasm project run under
  node/bun (see `WhoTestsTheTesters/Tests/BasicTests.Wasm.Mtp`); `mono`
  is named in platform ids; `Style.FSharp`'s README reframes it as the
  tests-as-values style now that module-level tests need nothing extra.

### 1.2 - 2026-09-16

- `AnyUnit.TestingPlatform` can write AnyUnit's own JSON results file
  (`--report-anyunit-json`), so the MTP path is a first-class results
  producer rather than depending on MTP's TRX extension for any file
  output at all - which in turn lets the MTP legs be gated by the same
  `ConventionTestProcessor` conformance check every other runner gets.
- Results schema carries what downstream formats actually need:
  separate exception message, stack trace and type, skip reason, and
  key-value properties alongside the existing flat categories. Additive -
  `SchemaVersion` stays at 1.
- `async Task` test methods are awaited. Previously the returned `Task`
  was discarded unawaited, so an async test's failures were silently
  lost and it passed regardless.
- `AnyUnit.Style.MsTest` - roughly MSTest-compatible attributes and
  assertions.
- `AnyUnit.Style.Expecto` - Expecto's value-based F# style: `testList`/
  `testCase` trees and the `Expect` vocabulary.
- `[RequiresCapability]`: a test can declare a runtime facility it needs
  (async continuations, timeout enforcement) and is reported Ignored -
  with the reason - where the platform lacks it, replacing a hard-coded
  category exclusion that lived in one host.
- `AnyUnit.TestingPlatform`'s `EnableAnyUnitRunner` now works from the
  package (the entry-point generator is a real package dependency), and
  a new `--platform-suffix` flag labels an MTP run the way the console
  runners' `-p` does. CI consumes the packed package to keep it that way.
- `AnyUnit.Style.FsUnit` gains `haveSubstring`; `AnyUnit.Style.Nunit`
  gains `[Property]`/`[Author]`; `AnyUnit.Style.MsTest` reports
  `[Owner]`/`[Priority]`/`[TestProperty]` as properties. Mono is named
  in platform ids (`mono6-osx-x64`) instead of `unknown`.


### 1.1 - 2026-09-13

Reporting and release plumbing.

- `AnyUnit.Report` (`anyunit-report`): converts a runner-produced JSON
  results file into JUnit XML, TRX, NUnit3 XML, xUnit2 XML, CTRF JSON,
  a self-contained HTML report, or a GitHub-flavored markdown summary.
  Merges multiple input files.
- `AnyUnit.TestingPlatform`: native TRX via MTP's real
  `ITrxReportCapability` extension mechanism.
- Dynamic RID-style platform ids (`net10-osx-arm64`), with an optional
  suffix to distinguish runners, so a merged results file can say which
  platforms actually went into it.
- The `.trx`-producing and coverage CI legs, and one consolidated
  cross-platform convention summary.

### 1.0 - 2026-09-12

First stable release: the `netstandard2.0` core, the NUnit/xUnit/FSharp/
FsUnit styles and constraints, the MTP adapter, the console/browser-wasm/
net48 runners, and publication to nuget.org.

## 2013 - Portable Class Libraries & Silverlight

Original target: PCL profiles and Silverlight, back when there was no
single runtime a library could target across desktop .NET, Silverlight,
and early mobile/WinRT - CI ran on CodeBetter/TeamCity.

## 2017 - .NET Standard 1.0

Retargeted the core libraries to `netstandard1.0` (later broadened as
support matured) - PCL profiles gave way to .NET Standard as the
one-library-many-runtimes story. CI moved to AppVeyor (Windows) and
Travis CI (Mono).

## Current - .NET Standard 2.0 core, .NET 10 & Blazor/browser-wasm runners

Core libraries retargeted to `netstandard2.0`. Runners target `net10.0`
directly, plus a real browser-wasm target (a Blazor WebAssembly host
driven headlessly, or - for `AnyUnit.Runner.Bootstrap` - a plain
console entry point run under `node`/`bun`, no browser needed at all).
Adds a [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro)
adapter (`AnyUnit.TestingPlatform`) for a `dotnet test`/`dotnet run`-
compatible entry point. CI moved to GitHub Actions.

See [`Readme.md`](Readme.md) for the current package list and layout.
