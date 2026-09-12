# AnyUnit.Runner

Standalone .NET tool (`anyunit-runner`) that discovers and runs
[AnyUnit](https://github.com/jbtule/AnyUnit) tests in one or more
assemblies you point it at - no test project of your own needed, no MTP/
`dotnet test` integration required.

## Install

```
dotnet tool install --global AnyUnit.Runner
```

(Not yet published to nuget.org - grab a prerelease build from the
repo's own `pack` workflow artifacts, or build from source:
`dotnet pack Runner/Platforms/net10/net10-runner.csproj`.)

## Usage

```
anyunit-runner run [-o|-output <file>] [-teamcity] <assembly.dll> [<assembly2.dll> ...]
```

- `-o`/`-output <file>` - also write the full results as JSON to `<file>`.
- `-teamcity` - print TeamCity service messages instead of the plain
  human-readable summary.
- Exit code 0 if every test passed (no `Fail`/`Error` results), 1
  otherwise.

For a browser-wasm test assembly (one with native/P/Invoke dependencies
that only build for that target), use
[`AnyUnit.Runner.BrowserWasm`](../browser-wasm-runner)'s `anyunit-browser-wasm`
instead - this runner loads assemblies directly into its own process,
which can't host a browser-wasm-only native build.
