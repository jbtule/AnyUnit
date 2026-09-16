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

## What hangs here (and looks like the runner hanging)

The host is a Blazor app on a single-threaded runtime, and the whole
test run is one synchronous call. Anything in a test body that *blocks
waiting for another thread* therefore never returns, and - since
timeouts cannot be enforced there either (see `[RequiresCapability]`) -
the run simply stops, with no output, which reads as the runner having
hung before the first test. Two traps found on real ports:

- **F# `Async.RunSynchronously`, even for a workflow that never
  suspends.** FSharp.Core checks `SynchronizationContext.Current`; with
  none (a console process) it runs the workflow inline, but Blazor has
  one, so it hands the workflow to the thread pool and blocks the caller
  on a wait handle that can never be signalled. `async { return x } |>
  Async.RunSynchronously` hangs. Use `Async.StartImmediateAsTask` (runs
  on the calling thread until the first real suspension, so a
  non-suspending workflow comes back completed) or hand the `Task` to
  the engine as the test's return value and mark the test
  `[<RequiresCapability(TestCapabilities.AsyncYield)>]`.
- **`.Result`/`.Wait()`/`GetAwaiter().GetResult()` on anything that
  actually suspends** - same reason. The engine itself never blocks on
  a returned `Task` here (see `AsyncTestResult`), but it cannot see a
  wait inside the body.

To find which test it is: the console runner and the MTP legs print
progress as they go, this host relays output only at the end - so run
the same assembly through `anyunit-runner` or, for a genuine wasm run
with per-test output, the node/bun MTP path described in
[`AnyUnit.TestingPlatform`'s README](../../../AnyUnit.TestingPlatform/README.md).
