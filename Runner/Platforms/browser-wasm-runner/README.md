# AnyUnit.Runner.BrowserWasm

Standalone .NET tool (`anyunit-browser-wasm`) that runs
[AnyUnit](https://github.com/jbtule/AnyUnit) tests inside a real,
headless-browser-driven browser-wasm host - for test assemblies with
native (P/Invoke) dependencies that only build for the browser-wasm
target, where [`AnyUnit.Runner`](../net10)'s plain in-process loading
can't run them at all.

## Install

```
dotnet tool install --global AnyUnit.Runner.BrowserWasm
```

(Prerelease builds of unreleased work are published as artifacts of the
repo's own `pack` workflow run, or build from source - see the repo's
own `browser-wasm-runner.csproj` for the required
`browser-wasm-runner-host` publish step first.)

## Usage

```
anyunit-browser-wasm run [-o|-output <file>] [-teamcity] <assembly.dll> [<assembly2.dll> ...]
```

Same flags as `anyunit-runner`. Drives a real headless Chromium (via
[PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp)) hosting
the target assemblies, dynamically loaded at runtime - no republish
step needed per run, and no copying the assemblies anywhere; they're
served directly from wherever they already live on disk.

If your test assembly has no browser-wasm-only native dependency, prefer
[`AnyUnit.Runner`](../net10)'s plain `anyunit-runner` instead - it runs
in-process, with none of a real browser's overhead.
