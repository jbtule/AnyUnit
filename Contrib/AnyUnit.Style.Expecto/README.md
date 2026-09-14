# AnyUnit.Style.Expecto

[Expecto](https://github.com/haf/expecto)-style F# tests for
[AnyUnit](https://github.com/jbtule/AnyUnit): `testList`/`testCase`
values and the `Expect` assertion vocabulary, close enough that an
existing Expecto suite's call sites usually don't change.

```fsharp
module MyTests
open AnyUnit.Style.Expecto

[<Tests>]
let tests =
    testList "math" [
        testList "addition" [
            testCase "handles negatives" <| fun () ->
                Expect.equal (-2 + -3) -5 "negatives should add"
        ]

        testCaseAsync "async work" <| async {
            let! v = async { return 42 }
            Expect.equal v 42 "should round-trip"
        }

        ptestCase "not ready yet" <| fun () ->
            failtest "reported Ignored, never run"
    ]
```

Add the assembly-level opt-in once, anywhere in the project:

```fsharp
[<assembly: AnyUnit.Style.Expecto.Discovery.ExpectoStyle>]
do ()
```

Then run it with any AnyUnit runner - `anyunit-runner`, `dotnet test`
via `AnyUnit.TestingPlatform`, or browser-wasm.

## Porting notes

**Argument order is Expecto's**, including the required message last:
`Expect.equal actual expected message`. That is preserved literally, even
where it reads backwards to someone arriving from NUnit or xUnit.

**`open Expecto` becomes `open AnyUnit.Style.Expecto`.** The builders are
`[<AutoOpen>]`, so that single line is usually the whole edit.

**Nesting is flattened**, because AnyUnit's model is a fixed
Assembly → Fixture → Test. The root `testList` names the fixture; every
leaf keeps the rest of its path, joined with `/` - which is how Expecto
prints them anyway:

```
fixture "math"
  test "addition/handles negatives"
```

Two bindings in one module become two fixtures, so `[<Tests>]` is the
grouping unit rather than the module.

## What's covered

`testList`, `testCase`, `testCaseAsync`, `testCaseTask`, and the
`ptest*` pending forms of each. `[<Tests>]` discovery.

`Expect`: `equal`, `notEqual`, `isTrue`, `isFalse`, `isNull`,
`isNotNull`, `isSome`, `isNone`, `isOk`, `isError`, `isEmpty`,
`isGreaterThan`, `isLessThan`, `stringContains`, `sequenceEqual`, `all`,
`throws`, `throwsT`, plus top-level `failtest`.

## What's not

**Focused tests** (`ftestCase`, `ftestList`, `[<FTests>]`) are
deliberately absent. Focusing is a hand-editing debugging aid that is a
bug once committed - Expecto itself ships `--fail-on-focused-tests` to
catch exactly that - so a stray one here is a **compile error** rather
than a silent change in which tests run.

**`testProperty`** needs FsCheck, which the core deliberately doesn't
depend on. **`Expect.isFasterThan`** and the rest of Expecto.Performance
are out for the same reason.

**`runTestsWithCLIArgs`** and Expecto's own CLI have no equivalent -
AnyUnit has its own runners, which is the point of the exercise.

**Parallel execution.** AnyUnit's engine runs tests sequentially by
design, so Expecto's default parallelism (and `Sequenced`/`ParallelWith`)
has nothing to configure.

## One AnyUnit addition

`requires` has no Expecto counterpart. Attribute-based styles declare a
platform requirement with `[RequiresCapability(...)]`; a value-based
style has no method or class to attach an attribute to, so the
requirement composes into the tree instead:

```fsharp
requires AnyUnit.Run.TestCapabilities.AsyncYield (
    testCaseAsync "genuinely suspends" <| async {
        do! Async.Sleep 1
        Expect.isTrue true "resumes where the platform can yield"
    })
```

Wrapped around a `testList`, it applies to every leaf beneath it. Tests
whose awaits all complete synchronously - the overwhelmingly common case
- need nothing.

## How assertions reach AnyUnit

`Expect.equal actual expected "msg"` is a free function with no receiver,
and keeping that call site is the whole point of the style. It asserts
through an ambient `IAssert` set and cleared around each test body, so
the counts are real: a test that makes no assertion at all still reports
`NoError` rather than a false `Success`, which is the distinction
AnyUnit's instance-scoped `Assert` exists to preserve.

It is deliberately **not** `Assert.GlobalStyle`, which hands back a
throwaway `Assert` whose count reaches nobody and flips a process-wide
flag degrading that same distinction for every other style in the run.

The practical consequence: `Expect` only works inside a running test.
Calling it from module initialisation, or from a helper invoked outside
a test body, raises a descriptive `InvalidOperationException` rather than
quietly doing nothing.
