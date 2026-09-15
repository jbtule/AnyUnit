module ExpectoTests.Basic

open System
open AnyUnit.Style.Expecto

// Names encode the expected outcome (_Success/_Fail/_Error/_Ignore/
// _NoError) - see WhoTestsTheTesters/ConventionTestProcessor. For this
// style the name checked is the leaf's own path, so the suffix goes on
// the innermost testCase, not on the enclosing testList.

[<Tests>]
let basics =
    testList "Basics" [
        testCase "equal_Success" <| fun () ->
            Expect.equal (2 + 2) 4 "addition should work"

        testCase "notEqual_Success" <| fun () ->
            Expect.notEqual 1 2 "one is not two"

        // Structural, not reference: two separately-allocated arrays with
        // the same contents are equal under F#'s `=`, which is what
        // Expecto's `equal` is. Object.Equals would say otherwise - and
        // did, in a port of a real suite, failing 20 tests with a message
        // that printed the same array twice.
        testCase "equalArray_Success" <| fun () ->
            Expect.equal [| "Hi"; "Hello" |] [| "Hi"; "Hello" |] "arrays compare structurally"

        testCase "equalArray_Fail" <| fun () ->
            Expect.equal [| 1; 2 |] [| 1; 3 |] "deliberately different"

        testCase "equal_Fail" <| fun () ->
            Expect.equal (2 + 2) 5 "deliberately wrong"

        testCase "isTrue_Success" <| fun () ->
            Expect.isTrue true "true is true"

        testCase "isFalse_Fail" <| fun () ->
            Expect.isFalse true "deliberately wrong"

        testCase "failtest_Fail" <| fun () ->
            failtest "deliberate failtest"

        // No Expect call at all: the assert count stays zero, which is the
        // distinction AnyUnit's instance-scoped IAssert exists to preserve
        // (see the root Readme). Proves Expect is genuinely reaching the
        // running test's own helper rather than a throwaway one - with a
        // global assert this would be indistinguishable from a real pass.
        testCase "noAssertions_NoError" <| fun () ->
            ()

        testCase "throwing_Error" <| fun () ->
            raise (NotSupportedException("deliberate"))

        testCase "ignored_Ignore" <| fun () ->
            raise (AnyUnit.IgnoreException("deliberately ignored"))
    ]

[<Tests>]
let nested =
    // Nesting is the thing Expecto has and AnyUnit's fixed
    // Assembly/Fixture/Test does not: the root list names the FIXTURE, and
    // everything below it becomes the leaf's slash-joined name, which is
    // how Expecto itself prints them. These report as fixture "Nested"
    // with tests "outer/inner/deep_Success" and so on.
    testList "Nested" [
        testList "outer" [
            testList "inner" [
                testCase "deep_Success" <| fun () ->
                    Expect.equal "deep" "deep" "nested test should run"
            ]
            testCase "shallow_Success" <| fun () ->
                Expect.isTrue true "sibling of a list should run too"
        ]

        // Same leaf name as the one above, under a different path - proves
        // each leaf gets its own identity. Sharing one UniqueName would
        // make ResultsFile.Add silently merge them into a single test.
        testList "other" [
            testCase "shallow_Success" <| fun () ->
                Expect.isTrue true "distinct from outer/shallow"
        ]
    ]

[<Tests>]
let assertions =
    testList "Assertions" [
        testCase "isSome_Success" <| fun () ->
            Expect.isSome (Some 1) "should be Some"

        testCase "isNone_Success" <| fun () ->
            Expect.isNone (None: int option) "should be None"

        testCase "isOk_Success" <| fun () ->
            Expect.isOk (Ok 1: Result<int, string>) "should be Ok"

        testCase "isError_Success" <| fun () ->
            Expect.isError (Error "e": Result<int, string>) "should be Error"

        testCase "sequenceEqual_Success" <| fun () ->
            Expect.sequenceEqual [ 1; 2; 3 ] [ 1; 2; 3 ] "sequences should match"

        testCase "sequenceEqual_Fail" <| fun () ->
            Expect.sequenceEqual [ 1; 2 ] [ 1; 2; 3 ] "deliberately wrong"

        testCase "stringContains_Success" <| fun () ->
            Expect.stringContains "hello world" "world" "should contain"

        testCase "isGreaterThan_Success" <| fun () ->
            Expect.isGreaterThan 5 1 "five beats one"

        testCase "isLessThan_Success" <| fun () ->
            Expect.isLessThan 1 5 "one loses to five"

        testCase "isEmpty_Success" <| fun () ->
            Expect.isEmpty ([]: int list) "should be empty"

        testCase "isNotNull_Success" <| fun () ->
            Expect.isNotNull (box "x") "should not be null"

        testCase "all_Success" <| fun () ->
            Expect.all [ 2; 4; 6 ] (fun n -> n % 2 = 0) "all even"

        testCase "throws_Success" <| fun () ->
            Expect.throws (fun () -> raise (InvalidOperationException())) "should throw"

        testCase "throwsT_Success" <| fun () ->
            Expect.throwsT<InvalidOperationException>
                (fun () -> raise (InvalidOperationException())) "should throw that type"

        // The wrong exception type is a FAILED expectation, not an error.
        testCase "throwsT_Fail" <| fun () ->
            Expect.throwsT<InvalidOperationException>
                (fun () -> raise (NotSupportedException())) "deliberately the wrong type"

        // An assertion failure inside the body under test is this test's
        // own verdict and must not be swallowed as "it threw, so it
        // passed" - otherwise Expect.throws would mask real failures.
        testCase "throwsDoesNotSwallowAssertions_Fail" <| fun () ->
            Expect.throws (fun () -> Expect.equal 1 2 "inner failure escapes") "outer"
    ]

[<Tests>]
let pending =
    testList "Pending" [
        // ptestCase: compiled, reported, not run.
        ptestCase "single_Ignore" <| fun () ->
            failtest "must never run"

        // ptestList marks everything beneath it, however deeply nested.
        ptestList "wholeList" [
            testList "still" [
                testCase "nested_Ignore" <| fun () ->
                    failtest "must never run either"
            ]
        ]
    ]

[<Tests>]
let asyncTests =
    testList "Async" [
        // Completes synchronously, so it runs everywhere including
        // browser-wasm.
        testCaseAsync "completed_Success" <| async {
            let! value = async { return 42 }
            Expect.equal value 42 "should round-trip"
        }

        // Proves the ambient IAssert survives an await: AsyncLocal flows
        // across the continuation where a ThreadStatic would be gone.
        testCaseAsync "afterAwait_Fail" <| async {
            let! value = async { return 1 }
            Expect.equal value 2 "deliberately wrong, after an await"
        }

        // Task.CompletedTask, deliberately, not Task.Run: Task.Run
        // schedules onto another thread, which single-threaded
        // browser-wasm does not have - it would never complete there and
        // the engine would (correctly) report an Error. The common case
        // for a task-returning test is one that has already finished by
        // the time it is handed back, and that runs everywhere.
        testCaseTask "task_Success" <| fun () ->
            Expect.isTrue true "task body should assert through the same helper"
            System.Threading.Tasks.Task.CompletedTask

        // The uncommon case, and the reason `requires` exists: this one
        // genuinely suspends, so it can never resume under a
        // single-threaded runtime. Attribute-based styles say this with
        // [RequiresCapability(TestCapabilities.AsyncYield)]; a value-based
        // style has no method to attach that to, so it composes into the
        // tree instead. Reported Ignored on browser-wasm, and runs
        // normally everywhere else.
        requires AnyUnit.Run.TestCapabilities.AsyncYield (
            testCaseAsync "genuinelySuspends_Success" <| async {
                do! Async.Sleep 1
                Expect.isTrue true "resumes on a platform that can yield"
            })
    ]

// A custom helper in Expecto's own idiom: throws on failure, does
// nothing on success. Under AnyUnit that success path is indistinguishable
// from asserting nothing - unless it registers itself with Expect.pass.
let private hasOkValue v x =
    match x with
    | Ok x when x = v -> Expect.pass ()
    | Ok x -> Tests.failtestf "Expected Ok(%A), was Ok(%A)." v x
    | Error x -> Tests.failtestf "Expected Ok, was Error(%A)." x

// The same helper WITHOUT Expect.pass - the shape a real ported suite has
// before any cleanup.
let private hasOkValueSilent v x =
    match x with
    | Ok x when x = v -> ()
    | Ok x -> Tests.failtestf "Expected Ok(%A), was Ok(%A)." v x
    | Error x -> Tests.failtestf "Expected Ok, was Error(%A)." x

[<Tests>]
let helpers =
    testList "Helpers" [
        testCase "passRegisters_Success" <| fun () ->
            hasOkValue 1 (Ok 1)

        testCase "passRegistersFailure_Fail" <| fun () ->
            hasOkValue 1 (Ok 2)

        // No Expect.pass and no escape hatch: correctly reported as having
        // asserted nothing, which is what it looks like from outside.
        testCase "silentHelper_NoError" <| fun () ->
            hasOkValueSilent 1 (Ok 1)
    ]

// The per-binding escape hatch: every leaf here that completes without
// throwing is Success, even the ones that never touch Expect. The
// failing ones must still fail - the hatch only ever upgrades NoError.
[<Tests; CompletionIsPass>]
let completionIsPass =
    testList "CompletionIsPass" [
        testCase "silentHelper_Success" <| fun () ->
            hasOkValueSilent 1 (Ok 1)

        testCase "nothingAtAll_Success" <| fun () ->
            ()

        testCase "stillFails_Fail" <| fun () ->
            hasOkValueSilent 1 (Ok 2)

        // The hatch has to fire AFTER an asynchronous body finishes, not
        // after the call that merely produced the Task - otherwise the
        // count is checked before the body has run.
        testCaseAsync "async_Success" <| async {
            let! v = async { return Ok 1 }
            hasOkValueSilent 1 v
        }

        testCaseTask "task_Success" <| fun () ->
            System.Threading.Tasks.Task.CompletedTask

        testCaseAsync "asyncStillFails_Fail" <| async {
            let! v = async { return Ok 2 }
            hasOkValueSilent 1 v
        }
    ]
