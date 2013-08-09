## Tests

 - **BasicTests** tests for PclUnit basic included operations
 - **ConstraintsTests** tests for `PclUnit.Constraints` assertion library
 - **SilverlightTests** tests compiled for silverlight to test running for single platforms
 - **NunitTests** tests in NUnit Style
 - **XunitTests** tests in xUnit Style

 ### RunTests
  - `RunTests.yml` config to for pclunit-runner for `TestAll`
  - `RunTests.codebetterci.yml` config for pclunit-runner to run platforms on codebetterci
  - `RunTests.mono.yml` config for pclunit-runner to run platforms on mono *(aggregating pclunit-runner doesn't work on mono yet)*
  - `RunTests.Mono.Manually.sh` script to manually run tests for each platform on mono