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

See [`AnyUnit.Runner.Bootstrap`](../Runner/Bootstrap) for the
smallest way to actually call `Runner.Create`/`RunAll` from your own
entry point, or [`AnyUnit.TestingPlatform`](../AnyUnit.TestingPlatform)
for a generated `dotnet test`-compatible one.
