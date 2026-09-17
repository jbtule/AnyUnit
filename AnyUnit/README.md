# AnyUnit

Core library for [AnyUnit](https://github.com/jbtule/AnyUnit): attribute
base classes, the assertion helper, and the actual test discovery/
execution engine every style package (`AnyUnit.Style.Nunit`,
`AnyUnit.Style.Xunit`, `AnyUnit.Style.FSharp`, `AnyUnit.Style.FsUnit`)
and runner (`AnyUnit.Runner`, `AnyUnit.Runner.BrowserWasm`,
`AnyUnit.Runner.Bootstrap`, `AnyUnit.TestingPlatform`) builds on.

You almost never reference this package directly - install a style
package instead (it references this one transitively) and write tests in
whichever framework's syntax that style ports. This package is what makes
that syntax runnable on any platform `netstandard2.0` reaches, including
browser-wasm.

## What lives here

- `TestFixtureAttributeBase` / `TestAttributeBase` / `TestFixtureDiscoveryAttributeBase` -
  the base classes a style's own attributes (`[TestFixture]`, `[Test]`,
  ...) derive from, and the hooks (`ParameterSets`, `TestInvoke`,
  `FixtureInit`, ...) that let a style plug its own row-data/generator
  attributes and invocation behavior into one shared discovery/execution
  pipeline.
- `AssertionHelper` / `IAssert` / `AssertionException` - the base a style's
  own assertion class derives from, and the exception types
  (`AssertionException`, `IgnoreException`) that drive a test's
  pass/fail/ignore outcome.
- `Runner` / `Fixture` / `Test` / `ParameterSet` / `Result` - the engine:
  `Runner.Create(platform, assemblies)` discovers fixtures and builds a
  `Test` per parameter-set combination; `RunAll` executes them and reports
  a `Result` (`Success`/`Fail`/`Error`/`Ignore`/`NoError`) for each.

## Asynchronous tests

A test method may return `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`
or an F# `Async<'T>`. The engine waits for it before deciding the test's
outcome, so a failed assertion inside an `async` body is reported `Fail`
(and `Assert.Ignore` reported `Ignore`) just as it would be in a
synchronous one, and `[Timeout]` still applies - it preempts a hung
`await` the same way it preempts a hung loop. An `async Task<bool>` test
gets the same "returned `false` means fail" handling as a plain `bool`
one.

The one platform limit is single-threaded browser-wasm: a test whose
awaits all complete synchronously (the overwhelmingly common case) runs
there normally, but one that genuinely suspends can never resume, since
its continuation needs the thread to yield back to the browser's event
loop. Declare that with
`[RequiresCapability(TestCapabilities.AsyncYield)]` and the engine
reports the test `Ignored` there, naming the missing facility, while it
runs normally everywhere else. An undeclared one gets a clear `Error`
rather than hanging the page.

`[RequiresCapability]` is read by the engine, not by any one runner, so
it works in every style and every host at once. The other values:
`TestCapabilities.Timeouts`, for tests that exist to prove `[Timeout]`
actually fires; and `TestCapabilities.Threads`, for a body that *blocks*
waiting on work that runs on another thread - `Task.Run(...).Wait()`,
`.Result`, F#'s `Async.RunSynchronously` under a synchronization context
(which Blazor has, so even a never-suspending workflow hangs there). The
engine can see a returned `Task`; it cannot see a wait inside the body,
and on single-threaded wasm nothing else can rescue it, so an undeclared
one hangs the run.

See [`AnyUnit.Runner.Bootstrap`](../Runner/Bootstrap) for the
smallest way to actually call `Runner.Create`/`RunAll` from your own
entry point, or [`AnyUnit.TestingPlatform`](../AnyUnit.TestingPlatform)
for a generated `dotnet test`-compatible one.
