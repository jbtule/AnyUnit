## Tests

Self-test assemblies - each one exercises one style/runner path for
real, discovered and run the same way an actual consumer's tests would
be.

 - **BasicTests** - tests for AnyUnit's own built-in `[TestFixture]`/`[Test]`
   (no style package - the no-extra-package-needed baseline).
 - **ConstraintsTests** - tests for `AnyUnit.Constraints`.
 - **BootstrapTests** / **BootstrapTests.Wasm** - proves
   `AnyUnit.Runner.Bootstrap` works end to end, on desktop and under
   browser-wasm (run via `node`/`bun`, no headless browser needed).
 - **`Style/`** - one pair of projects per style (`NunitTests`,
   `XunitTests`, `MsTestTests`, `FSharpTests`, `FsUnitTests`, and `ComboTests`/
   `ComboTests.FSharp` mixing more than one style's attributes in the same
   assembly):
   - the plain project runs against `anyunit-runner`/`anyunit-browser-wasm`
     directly.
   - the `.Mtp` counterpart (e.g. `NunitTests.Mtp`) compiles the same
     source in, but with `EnableAnyUnitRunner=true` - proving the same
     tests also work through `AnyUnit.TestingPlatform`'s generated
     `dotnet test`/`dotnet run` entry point.
 - `.Mtp` projects also exist for `BasicTests`/`ConstraintsTests`
   (`BasicTests.Mtp`, `ConstraintsTests.Mtp`), same reasoning.

See `.github/scripts/run-tests.sh` for how CI drives each of these
through a built runner and `WhoTestsTheTesters/ConventionTestProcessor`.
