# Changes

A high-level catalog of the eras this repo has gone through, not a
per-commit changelog - each one was a real, working target at the time,
not an abandoned experiment.

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
