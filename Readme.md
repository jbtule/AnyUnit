# AnyUnit

Write tests in roughly the syntax your codebase already uses (NUnit,
xUnit, FsUnit, or F#'s own value-based style) against a small, shared
core, and run them on whichever platform actually needs them (desktop
.NET, `dotnet test`/Microsoft.Testing.Platform, or browser-wasm) -
without a separate test framework or test project per platform.

[![build](https://github.com/jbtule/AnyUnit/actions/workflows/build.yml/badge.svg)](https://github.com/jbtule/AnyUnit/actions/workflows/build.yml)

## Why

A shared `netstandard2.0` library can run on far more platforms than any
one full test framework does. AnyUnit's core (`AnyUnit`) targets
`netstandard2.0` itself, and each "style" package aims for close enough
syntax compatibility with real NUnit/xUnit/FsUnit that an existing test's
*logic* - its attributes and assertions - often doesn't need to change at
all: `Assert.That(...)` inside a test method carries over as-is once the
fixture class inherits `AssertionHelper`, since `Assert` there just
resolves to that instance automatically, the same identifier real NUnit
uses. Where it holds, moving an existing NUnit test suite onto AnyUnit's
`AnyUnit.Style.Nunit` is mostly a `PackageReference` swap plus a `using`
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
instance scoped to exactly one test's own run tracks that correctly
(AnyUnit does still offer a global-style escape hatch, `Assert.
GlobalStyle`, but it's `[Obsolete]` for exactly this reason - it's there
for genuinely global helper code, not as the default way to assert).

## Packages

| Package | What it is |
|---|---|
| [`AnyUnit`](AnyUnit) | Core: attribute base classes, assertion helper, test discovery/execution engine. Every style package builds on this. |
| [`AnyUnit.Constraints`](Contrib/AnyUnit.Constraints) | NUnit-style fluent `Is`/`Has`/`Does`/`Throws` constraint syntax. |
| [`AnyUnit.Style.Nunit`](Contrib/AnyUnit.Style.Nunit) | Roughly NUnit-compatible attributes (`[Test]`, `[TestCase]`, `[SetUp]`, `[TestFixture]`, ...) and assertions - close enough syntax to often keep a test's logic unchanged, not a full reimplementation. |
| [`AnyUnit.Style.Xunit`](Contrib/AnyUnit.Style.Xunit) | Roughly xUnit-compatible attributes (`[Fact]`, `[Theory]`, `[InlineData]`, ...) and assertions - same caveat. |
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
  `AnyUnit.Style.FsUnit`) - each one gets close enough to a specific,
  real test framework's own syntax that an existing test's logic often
  doesn't need to change to move onto `AnyUnit`'s core - not a full
  reimplementation of that framework's API (see each style's own README
  for what's actually covered).
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
