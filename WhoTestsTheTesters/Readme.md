## WhoTestsTheTesters

AnyUnit's own test suite - tests for the core library and each style,
written *in* that style, run through the real runners. A regression in
discovery/execution shows up the same way it would for an actual
consumer, not just via a hand-written unit test of the discovery code
itself.

- **`Tests/`** - the self-test assemblies themselves (see its own README).
- **`ConventionTestProcessor`** - reads a runner's JSON results file and
  verifies each test passed/failed/errored/ignored exactly as its own
  name says it should (`TestX_Success`, `TestY_Fail`, ...) - the actual
  pass/fail gate CI checks, not the runner's own exit code (which is
  expected to be non-zero, on purpose: these self-test assemblies always
  contain real `Fail`/`Error` cases by design).
- **`RoughRunner`** - a minimal, dependency-light runner used for local
  debugging.

See `.github/scripts/run-tests.sh` for how CI actually drives a self-test
assembly through a built runner (`anyunit-runner`/`anyunit-browser-wasm`)
and into `ConventionTestProcessor`.
