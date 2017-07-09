## Tests

 - **BasicTests** tests for AnyUnit basic included operations
 - **ConstraintsTests** tests for `AnyUnit.Constraints` assertion library
 - **SilverlightTests** tests compiled for silverlight to test running for single platforms
 - **NunitTests** tests in NUnit Style
 - **XunitTests** tests in xUnit Style

 ### RunTests
  - `RunTests.yml` config to for anyunit-runner for `TestAll`
  - `RunTests.codebetterci.yml` config for anyunit-runner to run platforms on codebetterci
  - `RunTests.mono.yml` config for anyunit-runner to run platforms on mono *(aggregating anyunit-runner doesn't work on mono yet)*
  - `RunTests.Mono.Manually.sh` script to manually run tests for each platform on mono