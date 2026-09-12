module AnyUnit.Style.FSharp.Runner

open System
open AnyUnit
open AnyUnit.Run
open AnyUnit.Style.FSharp.Test

/// Runs a list of Test values, folding them into an AnyUnit ResultsFile -
/// the same output shape (and JSON) every reflection-discovered style
/// produces, so this plugs straight into WriteResults.ToFiles and
/// everything downstream of it (ConventionTestProcessor, the console
/// runners' own summary printing) unchanged. Reuses AnyUnit.Run.Assert/
/// Log/TestCycleExceptions directly - the exact same exception ->
/// ResultKind classification (Ignore/Fail/Error/Success/NoError) and
/// log capture every other style gets, just invoking a Test's own thunk
/// instead of a MethodInfo.
///
/// fixtureName groups every test under one FixtureMeta (matching a
/// single [<TestFixture>]-equivalent) - pass the module/file name, or
/// whatever grouping makes sense; there's no reflection-derived type
/// name to fall back on here. Each test's own name likewise has no
/// reflection-derived source in this direct-call path (unlike
/// Discovery.fs, which reads it straight off the property) - the
/// simplest thing is just asking the caller for it alongside the Test
/// itself, e.g. `run "net10" "MyTests" [ "a passing test", passingTest ]`.
let run (platformId: string) (fixtureName: string) (tests: (string * Test) list) : ResultsFile =
    let file = ResultsFile()
    let assemblyMeta = AssemblyMeta(Name = fixtureName, UniqueName = "A:" + fixtureName)
    let fixtureMeta = FixtureMeta(Name = fixtureName, UniqueName = "T:" + fixtureName)
    assemblyMeta.Fixtures.Add(fixtureMeta)

    for (name, t) in tests do
        let testMeta = TestMeta(Name = name, UniqueName = fixtureName + "." + name)
        fixtureMeta.Tests.Add(testMeta)

        let startTime = DateTime.Now
        let helper = AssertionHelper(Assert = Assert(), Log = Log())
        let exceptions = TestCycleExceptions()

        try
            t.Run helper
        with ex ->
            exceptions.Add(TestCycle.Test, ex)

        let kind = exceptions.GetResult(helper)
        exceptions.WriteOutExceptions(helper)
        let result = Result(platformId, kind, startTime, DateTime.Now, helper)
        testMeta.Results.Add(result)
        file.Add(result)

    file
