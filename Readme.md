### Pcl Unit
Write Once, Test Everywhere

[Portable Class Libraries][pcl] allow you to code to a single dll that can run on multiple .net or .net like platforms.

**Problem**, if you want to test for these platforms you have to use a testing framework specific for each platform and compile your unit tests specifically for each platform.

**Solution**, The goal of PclUnit is to target the broadest PCL profile and implement both describing tests and executing tests in the profile. To have runner executables specific for each platform which can consolidate results in a clear and consistent way.

See [Pcl Unit Design and Philosophy][Design].

####Layout
 - **Contrib**: Basic Extensions on Assertion and Test Discovery ported from other Frameworks
   **PclUnit**: Core Assertion, Discovery and Runner library
 - **Runner**: Aggregating runner and platform specfic runners
   **WhoTestsTheTesters**: Code to test Tests

####Status
Alpha Version
  - Design wise can still fluctuate, but functional and needs more coverage and platforms
  - PclUnit, Core Libraries and Basic Test Runner -Win & Mono
  - pclunit-runner, Aggregating Test Runner -Win only
      - net40 x86 & x64 platform runners - Win & Mono
      - net45 x86, x64 platform runners - Win & Mono
      - sl50 x86, x64 platform runners - Win Only
  - PclUnit.Constraints, (port of Nunit Constraints)
  - PclUnit.Style.Xunit, (port of xUnit test attributes, discovery & assertions)
  - PclUnit.Style.Nunit, (port of NUnit test attributes, discover)

[travis-ci]:https://travis-ci.org/
[Design]:http://github.com/jbtule/PclUnit/wiki/Design
[pcl]:http://msdn.microsoft.com/en-us/library/gg597391.aspx

| Status | Provider |
| --- | --- |
| [![Build Status][WinImg]][WinLink] | Windows CI Provided By [CodeBetter][] and [JetBrains][] |
| [![Build Status][MonoImg]][MonoLink] | Mono CI Provided by [travis-ci][] |


[WinImg]:http://teamcity.codebetter.com/app/rest/builds/buildType:(id:bt1048)/statusIcon?updatecache=1
[WinLink]:http://teamcity.codebetter.com/viewLog.html?buildTypeId=bt1048&buildId=lastFinished&guest=1
[JetBrains]:http://www.jetbrains.com/
[CodeBetter]:http://codebetter.com/
[MonoImg]:https://travis-ci.org/jbtule/PclUnit.png?branch=master
[MonoLink]:https://travis-ci.org/jbtule/PclUnit