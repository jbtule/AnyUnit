# AnyUnit

Write tests in roughly the syntax your codebase already uses (NUnit,
xUnit, FsUnit, or F#'s own value-based style) against a small, shared
core, and run them on whichever platform actually needs them (desktop
.NET, `dotnet test`/Microsoft.Testing.Platform, or browser-wasm) -
without a separate test framework, and without rewriting or duplicating
the tests themselves per platform.

[![build](https://github.com/jbtule/AnyUnit/actions/workflows/build.yml/badge.svg)](https://github.com/jbtule/AnyUnit/actions/workflows/build.yml)

> **Not mature yet.** The 1.x line is real and released, but it is
> moving fast: APIs, package layout and even reported behaviour (what a
> platform id looks like, which assert a free function goes through)
> have all changed between point releases, and will keep doing so as
> real suites get ported onto it and find things. Pin a version, read
> [`Changes.md`](Changes.md) before bumping, and expect to touch your
> test project when you do. See "Status" below.

**Design philosophy:** keep the tests you already have, not rewrite them
- write them once, and don't have to keep rewriting them as the platforms
you need to run on change over time - and run those same tests on every
platform you need without a separate test framework or a hand-maintained
duplicate suite per target. A thin per-platform host project sometimes
still exists (an MTP-enabled satellite, a browser-wasm entry point - see
`WhoTestsTheTesters/Tests` for real examples), but it's a few lines
wiring an existing suite up, not a second copy of the tests themselves.

That reach is also why the discovery/execution engine (`AnyUnit`'s own
`Runner`/`Fixture`/`Test`) is deliberately kept simple: plain reflection
(`Type.GetMethods`, `GetCustomAttributes`, `Assembly.GetTypes`) - nothing
fancier, no `Reflection.Emit`, no runtime IL/expression-tree compilation.
Constrained runtimes (Mono's interpreter under browser-wasm, most
concretely) don't reliably support that fancier machinery in the first
place, so staying with what plain reflection can already do is what
makes running everywhere possible, not an accident.

## Why

A shared `netstandard2.0` library can run on far more platforms than any
one full test framework does. AnyUnit's core (`AnyUnit`) targets
`netstandard2.0` itself, and each "style" package aims for close enough
syntax compatibility with real NUnit/xUnit/FsUnit that an existing test's
*logic* - its attributes and assertions - often doesn't need to change:
`Assert.That(...)` inside an ordinary test method usually carries over
unchanged once the fixture class inherits `AssertionHelper`, since
`Assert` there resolves to that instance automatically, the same
identifier real NUnit uses. Where it holds, moving an existing NUnit
test suite onto AnyUnit's `AnyUnit.Style.Nunit` is mostly a
`PackageReference` swap plus a `using`
directive swap - confirmed by actually doing it on a real, ~140-test
NUnit suite. The other edits that suite needed were real but narrow: a
few real-NUnit idioms this repo deliberately doesn't reproduce
(`TestContext`; a fully-static `Assert`, for reasons below) only came up
in a couple of non-fixture helper classes, not in ordinary test methods.
Once that's done, it actually runs somewhere a real NUnit install can't
reach - most concretely, browser-wasm today.

AnyUnit's `Assert` is deliberately an instance a fixture gets (not a
fully-static class the way real NUnit's is): a shared/global assert can't
reliably tell a test that made real assertions and passed apart from one
that made none at all and trivially "passed" by doing nothing - an
instance scoped to exactly one test's own run tracks that correctly.
Code with no `this` to assert through - a module-level F# `let` test, a
free-function vocabulary like FsUnit's `should` - reaches that same
instance via `AnyUnit.Run.AmbientTest.Current` (or `Assert.Current` for
the IAssert alone), which the engine sets around every test body. The
older `Assert.GlobalStyle` still exists but is `[Obsolete]` for exactly
the reason above: it hands back a throwaway assert and degrades the
distinction for the whole run.

Styles can also be mixed in one project, not just chosen between: a
single assembly can have both NUnit-style and xUnit-style fixtures side
by side, and even one `[Theory]` method combining NUnit's `[TestCase]`
rows with xUnit's `[InlineData]` rows, or NUnit's `[Values]` driving an
xUnit `[Theory]`'s own parameters. `WhoTestsTheTesters/Tests/Style/ComboTests`
(and its F# counterpart, `ComboTests.FSharp`) is real, running proof of
this, not just a claim.

## Platform coverage

Real test-framework compatibility gaps often only show up on a specific
platform, not in general - the whole reason to actually run somewhere
instead of assuming. CI runs AnyUnit's own test suite for real (not just
compiles it) across the platforms below:

- `net10.0` self-contained on 7 real RIDs actually executed in CI
  (`win-x64`, `win-x86`, `win-arm64`, `linux-x64`, `linux-arm64`,
  `osx-arm64`, `osx-x64`), so a platform-specific edge case (a
  32-bit-only bug, an ARM-vs-x64 difference) has somewhere to actually
  surface instead of only ever running on whatever the CI host happens
  to be. `linux-arm` (32-bit ARM, what a Raspberry Pi 2 needs) is still
  built and published for every release - it just isn't executed in CI
  itself, since a 32-bit armhf binary can't run on the aarch64 host CI
  uses for ARM without extra emulation setup not currently in place.
- `browser-wasm`, via a real headless browser (see
  `AnyUnit.Runner.BrowserWasm`) - Mono/wasm's single-threaded runtime
  and reflection quirks are a genuinely different execution environment,
  not just "the same .NET on a different OS."
- `net48`, for a legacy .NET Framework target still in real use.

See [`build.yml`](.github/workflows/build.yml) for exactly which RID
runs on which real hosted runner (an ARM RID gets a real ARM host where
one exists, not just cross-compiled and assumed to work).

## Packages

| Package | What it is |
|---|---|
| [`AnyUnit`](AnyUnit) | Core: attribute base classes, assertion helper, test discovery/execution engine. Every style package builds on this. |
| [`AnyUnit.Constraints`](Contrib/AnyUnit.Constraints) | NUnit-style fluent `Is`/`Has`/`Does`/`Throws` constraint syntax. |
| [`AnyUnit.Style.Nunit`](Contrib/AnyUnit.Style.Nunit) | Roughly NUnit-compatible attributes (`[Test]`, `[TestCase]`, `[SetUp]`, `[TestFixture]`, ...) and assertions - close enough syntax to often keep a test's logic unchanged, not a full reimplementation. |
| [`AnyUnit.Style.Xunit`](Contrib/AnyUnit.Style.Xunit) | Roughly xUnit-compatible attributes (`[Fact]`, `[Theory]`, `[InlineData]`, ...) and assertions - same caveat. |
| [`AnyUnit.Style.MsTest`](Contrib/AnyUnit.Style.MsTest) | Roughly MSTest-compatible attributes (`[TestClass]`, `[TestMethod]`, `[DataRow]`, `[TestInitialize]`, ...) and assertions - same caveat. |
| [`AnyUnit.Style.FSharp`](Contrib/AnyUnit.Style.FSharp) | F#'s own idiomatic value-based test style (`test { }`), for when a `[Test]`-attributed method doesn't fit F# as well as a top-level `let` does. |
| [`AnyUnit.Style.FsUnit`](Contrib/AnyUnit.Style.FsUnit) | FsUnit-style F# assertions. |
| [`AnyUnit.Style.Expecto`](Contrib/AnyUnit.Style.Expecto) | Expecto's value-based F# style - `testList`/`testCase` trees and the `Expect` vocabulary. |
| [`AnyUnit.TestingPlatform`](AnyUnit.TestingPlatform) | Microsoft.Testing.Platform (MTP) adapter - opt in with one MSBuild property (`EnableAnyUnitRunner`) to get a real `dotnet test`/`dotnet run` entry point generated for you. |
| [`AnyUnit.Runner.Bootstrap`](Runner/Bootstrap) | A single static `Runner.Run(platform)` a consumer's own `Main` calls directly - the smallest way to get a real, runnable test entry point (desktop or browser-wasm) without MTP or a CLI. |
| [`AnyUnit.Runner`](Runner/Platforms/net10) | Standalone CLI (`anyunit-runner`) that discovers and runs tests in one or more assemblies you point it at. |
| [`AnyUnit.Runner.BrowserWasm`](Runner/Platforms/browser-wasm-runner) | Same CLI shape as `AnyUnit.Runner`, but runs the target assemblies inside a real headless-browser-driven browser-wasm host - for test assemblies with native (P/Invoke) dependencies that only build for the browser-wasm target. |
| [`AnyUnit.Report`](Report) | Standalone CLI (`anyunit-report`) that converts an `anyunit-runner`-produced JSON results file into JUnit XML, TRX, NUnit3 XML, xUnit2 XML, or [CTRF](https://ctrf.io) JSON - whatever your CI system or dashboard already understands. |

Every package above is on [nuget.org](https://www.nuget.org) once a tagged
release goes out (see "Status" below for where things stand before then):

```bash
dotnet add package AnyUnit
dotnet add package AnyUnit.Style.Nunit   # or .Style.Xunit / .Style.MsTest / .Style.FSharp / .Style.FsUnit / .Constraints
dotnet add package AnyUnit.TestingPlatform  # for a dotnet test/dotnet run entry point
dotnet add package AnyUnit.Runner.Bootstrap # or write your own Main directly

dotnet tool install --global AnyUnit.Runner            # anyunit-runner
dotnet tool install --global AnyUnit.Runner.BrowserWasm # anyunit-browser-wasm
dotnet tool install --global AnyUnit.Report             # anyunit-report
```

A tagged release's own [GitHub Release](https://github.com/jbtule/AnyUnit/releases)
page also carries every CLI tool's `.nupkg` file (`anyunit-runner`,
`anyunit-browser-wasm`, `anyunit-report`), a self-contained, single-file
`anyunit-runner` executable for every RID in "Platform coverage" above,
and the standalone `net48` runner `.exe` (32- and 64-bit), if you'd
rather download one directly than install it.

## Layout

- **`AnyUnit`** - the core library every style and runner depends on:
  attribute base classes (`TestAttributeBase`, `TestFixtureAttributeBase`,
  ...), `AnyUnit.Run.AssertionHelper`/`IAssert`, the actual discovery/
  execution engine (`Runner`, `Fixture`, `Test`, `ParameterSet`), and its
  own built-in attribute style in `AnyUnit.Style.Core`.
- **`Contrib`** - the style packages (`AnyUnit.Constraints`,
  `AnyUnit.Style.Nunit`, `AnyUnit.Style.Xunit`, `AnyUnit.Style.MsTest`,
  `AnyUnit.Style.FSharp`, `AnyUnit.Style.FsUnit`) - each one gets close enough to a specific,
  real test framework's own syntax that an existing test's logic often
  doesn't need to change to move onto `AnyUnit`'s core - not a full
  reimplementation of that framework's API (see each style's own README
  for what's actually covered).
- **`AnyUnit.TestingPlatform`** - the Microsoft.Testing.Platform adapter.
  **`AnyUnit.TestingPlatform.WasmEntry`** beside it is the awaitable
  entry point that ships inside that package for F# test projects on
  browser-wasm (see the adapter's README).
- **`Runner`** - every runner *except* the MTP adapter above:
  - **`Runner/Bootstrap`** - the minimal `Runner.Run(platform)` library
    form, for a consumer that wants a real entry point with no CLI/MTP
    involved (an F# project's own hand-written `Program.fs`, for example
    - see its own README for why).
  - **`Runner/Platforms`** - the standalone CLI runners (`anyunit-runner`,
    `anyunit-browser-wasm`) and the shared argument-parsing/output-
    formatting code (`Runner/Platforms/shared`) all three of these
    (including `Runner/Bootstrap`) build on. `Runner/Platforms/support/AnyUnit.BrowserRunner`
    is the Razor component (results table, log) `browser-wasm-runner-host`'s
    Blazor WASM app is built from - internal plumbing for that one host,
    not published as its own package, grouped under `support/` alongside
    `shared` rather than as a directly-runnable platform itself.
- **`Report`** - the standalone CLI (`anyunit-report`) that converts an
  `anyunit-runner`-produced JSON results file into JUnit XML, TRX,
  NUnit3 XML, xUnit2 XML, or CTRF JSON.
- **`WhoTestsTheTesters`** - AnyUnit's own test suite: tests for the core
  library and each style, written *in* that style, run through the real
  runners - so a regression in discovery/execution shows up the same way
  it would for an actual consumer.
- **`deploy`** - packaging/release-adjacent scripts and configuration.

## Status

1.2 is released and on nuget.org; 1.2.1 is in development (see
[`Changes.md`](Changes.md) for per-release notes). **Treat all of 1.x as
pre-stabilisation**: releases are frequent, and each one so far has
changed something a consumer can see - the results schema grew, the
MTP adapter's platform label changed, FsUnit's free-function `should`
stopped being global - because each was driven by porting a real
third-party suite and fixing what it hit. That is the intended way for
this to mature, and it means a minor or patch version bump is not yet
a promise of source or behavioural compatibility. This is a genuinely
old project - the core ideas here go back 13 years, to a
PCL/Silverlight-era predecessor - and getting to 1.0 meant catching up a
lot of that history in one pass, so expect some rough edges: a style
covering less of its real framework than you'd want, docs that lag a
recent change, a corner nothing's exercised for real yet. Two things are
solid, though, because they've actually been exercised, not assumed: the
discovery/execution engine is genuinely portable (real desktop RIDs,
browser-wasm, MTP - see "Platform coverage" above), and the style
mechanism is genuinely extensible (`IRowInlineParameter`/
`IGeneratingParameter`/`IArgParameter` let one style recognize another's
own attributes with no reference between them - see "Why" above); adding
a new style builds on the existing base classes rather than changing how
they behave, though core does occasionally gain a new extension point
like those interfaces to make something like that possible in the first
place.

It's free (Apache-2.0) and useful for what it actually does - reach for
it when you need tests to run somewhere a real NUnit/xUnit/MTP install
can't, not as a general NUnit/xUnit replacement for a project that only
ever targets one desktop platform.

A tagged release pushes every package to nuget.org (real Trusted
Publishing, no long-lived credential involved) and attaches the CLI
tools' `.nupkg` files and every RID's `anyunit-runner` executable
directly to that release. Before (or between) tags, the `pack` job in
[`build.yml`](.github/workflows/build.yml) still builds and uploads
the same packages as workflow artifacts on every push, so a recent
prerelease build is normally there to grab and try without waiting on
a release, subject to GitHub Actions' own artifact retention window.
Versions follow [MinVer](https://github.com/adamralph/minver) off `v*`
tags (e.g. `v1.0.0-alpha` → `1.0.0-alpha.0.<commits-since>` for an
untagged build).

## License

[Apache-2.0](License.txt). See [`Contributors.md`](Contributors.md) for
attribution, including the real NUnit/xUnit source this repo ports from,
and [`Changes.md`](Changes.md) for the eras this repo's own target
platform has gone through (PCL/Silverlight → .NET Standard 1.0 → today's
.NET Standard 2.0 core with .NET 10/browser-wasm runners).
