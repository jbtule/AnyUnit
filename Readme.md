# AnyUnit

Write tests once against a small, source-compatible core - run them under
whichever test style your codebase already uses (NUnit, xUnit, FsUnit, or
F#'s own value-based style), on whichever platform actually needs to run
them (desktop .NET, `dotnet test`/Microsoft.Testing.Platform, or
browser-wasm) - without a separate test framework or test project per
platform.

[![build](https://github.com/jbtule/AnyUnit/actions/workflows/build.yml/badge.svg)](https://github.com/jbtule/AnyUnit/actions/workflows/build.yml)

## Why

A shared `netstandard2.0` library can run on far more platforms than any
one full test framework does. AnyUnit's core (`AnyUnit`) targets
`netstandard2.0` itself, and each "style" package layers real NUnit/xUnit/
FsUnit source compatibility on top of it - so an existing NUnit test suite
can often move to AnyUnit's `AnyUnit.Style.Nunit` with only its
`PackageReference`s changed, not its test source, and then actually run
somewhere a real NUnit/xUnit install can't reach - most concretely,
browser-wasm today.

## Packages

| Package | What it is |
|---|---|
| [`AnyUnit`](AnyUnit) | Core: attribute base classes, assertion helper, test discovery/execution engine. Every style package builds on this. |
| [`AnyUnit.Constraints`](Contrib/AnyUnit.Constraints) | NUnit-style fluent `Is`/`Has`/`Does`/`Throws` constraint syntax. |
| [`AnyUnit.Style.Nunit`](Contrib/AnyUnit.Style.Nunit) | NUnit-source-compatible attributes (`[Test]`, `[TestCase]`, `[SetUp]`, `[TestFixture]`, ...) and assertions. |
| [`AnyUnit.Style.Xunit`](Contrib/AnyUnit.Style.Xunit) | xUnit-source-compatible attributes (`[Fact]`, `[Theory]`, `[InlineData]`, ...) and assertions. |
| [`AnyUnit.Style.FSharp`](Contrib/AnyUnit.Style.FSharp) | F#'s own idiomatic value-based test style (`test { }`), for when a `[Test]`-attributed method doesn't fit F# as well as a top-level `let` does. |
| [`AnyUnit.Style.FsUnit`](Contrib/AnyUnit.Style.FsUnit) | FsUnit-style F# assertions. |
| [`AnyUnit.TestingPlatform`](AnyUnit.TestingPlatform) | Microsoft.Testing.Platform (MTP) adapter - opt in with one MSBuild property (`EnableAnyUnitRunner`) to get a real `dotnet test`/`dotnet run` entry point generated for you. |
| [`AnyUnit.Runner.Bootstrap`](Runner/Bootstrap) | A single static `Runner.Run(platform)` a consumer's own `Main` calls directly - the smallest way to get a real, runnable test entry point (desktop or browser-wasm) without MTP or a CLI. |
| [`AnyUnit.Runner`](Runner/Platforms/net10) | Standalone CLI (`anyunit-runner`) that discovers and runs tests in one or more assemblies you point it at. |
| [`AnyUnit.Runner.BrowserWasm`](Runner/Platforms/browser-wasm-runner) | Same CLI shape as `AnyUnit.Runner`, but runs the target assemblies inside a real headless-browser-driven browser-wasm host - for test assemblies with native (P/Invoke) dependencies that only build for the browser-wasm target. |

## Layout

- **`AnyUnit`** - the core library every style and runner depends on:
  attribute base classes (`TestAttributeBase`, `TestFixtureAttributeBase`,
  ...), `AssertionHelper`/`IAssert`, and the actual discovery/execution
  engine (`Runner`, `Fixture`, `Test`, `ParameterSet`).
- **`Contrib`** - the style packages (`AnyUnit.Constraints`,
  `AnyUnit.Style.Nunit`, `AnyUnit.Style.Xunit`, `AnyUnit.Style.FSharp`,
  `AnyUnit.Style.FsUnit`) - each one layers a specific, real test
  framework's source-level API on top of `AnyUnit`'s core attributes.
- **`AnyUnit.TestingPlatform`** - the Microsoft.Testing.Platform adapter.
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
- **`WhoTestsTheTesters`** - AnyUnit's own test suite: tests for the core
  library and each style, written *in* that style, run through the real
  runners - so a regression in discovery/execution shows up the same way
  it would for an actual consumer.
- **`deploy`** - packaging/release-adjacent scripts and configuration.

## Status

Pre-1.0, actively developed. Packages aren't published to nuget.org yet -
every push builds and uploads them as workflow artifacts (see the `pack`
job in [`build.yml`](.github/workflows/build.yml)), so a current
prerelease build is always available to grab and try without waiting on a
tagged release. Versions follow [MinVer](https://github.com/adamralph/minver)
off `v*` tags (e.g. `v1.0.0-alpha` → `1.0.0-alpha.0.<commits-since>` for an
untagged build).

## License

[Apache-2.0](License.txt). See [`Contributors.md`](Contributors.md) for
attribution, including the real NUnit/xUnit source this repo ports from,
and [`Changes.md`](Changes.md) for the eras this repo's own target
platform has gone through (PCL/Silverlight → .NET Standard 1.0 → today's
.NET Standard 2.0 core with .NET 10/browser-wasm runners).
