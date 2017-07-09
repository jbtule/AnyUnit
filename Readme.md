### AnyUnit
Write Once, Test Everywhere

[Portable Class Libraries][pcl] allow you to code to a single dll that can run on multiple .net or .net like platforms.

**Problem**, if you want to test for these platforms you have to use a testing framework specific for each platform and compile your unit tests specifically for each platform.

**Solution**, The goal of AnyUnit is to target the broadest PCL profile and implement both describing tests and executing tests in the profile. To have runner executables specific for each platform which can consolidate results in a clear and consistent way.

See [Pcl Unit Design and Philosophy][Design].

#### Layout
 - **Contrib**: Basic Extensions on Assertion and Test Discovery ported from other Frameworks
 - **AnyUnit**: Core Assertion, Discovery and Runner library
 - **Runner**: Aggregating runner and platform specfic runners
 - **WhoTestsTheTesters**: Code to test Tests

#### Status 
Windows: [![Build Status][WinImg]][WinLink] Mono: [![Build Status][MonoImg]][MonoLink] 

Beta Version
  - Design wise can still fluctuate, but functional and needs more coverage and platforms
  - AnyUnit, Core Libraries and Basic Test Runner -Win & Mono
  - anyunit-runner (alpha Version), Aggregating Test Runner Win & Mono
      - net40 x86 & x64 platform runners - Win & Mono
      - net45 x86, x64 platform runners - Win & Mono
      - sl50 x86, x64 platform runners - Win Only
  - AnyUnit.Constraints, (port of Nunit Constraints)
  - AnyUnit.Style.Xunit, (port of xUnit test attributes, discovery & assertions)
  - AnyUnit.Style.Nunit, (port of NUnit test attributes, discover)
  - AnyUnit.Style.FSUnit, (port of FsUnit test assertions)

[travis-ci]:https://travis-ci.org/
[Design]:http://github.com/jbtule/AnyUnit/wiki/Design
[pcl]:http://msdn.microsoft.com/en-us/library/gg597391.aspx


[WinImg]:https://ci.appveyor.com/api/projects/status/l8h3jw6c7d332y02?svg=true
[WinLink]:https://ci.appveyor.com/project/jbtule/anyunit
[JetBrains]:http://www.jetbrains.com/
[CodeBetter]:http://codebetter.com/
[MonoImg]:https://travis-ci.org/jbtule/AnyUnit.png?branch=master
[MonoLink]:https://travis-ci.org/jbtule/AnyUnit
