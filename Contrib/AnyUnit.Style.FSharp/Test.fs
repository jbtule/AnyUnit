module AnyUnit.Style.FSharp.Test

open System
open AnyUnit

/// The computation a test body runs as: a function from the shared
/// per-test assertion/log context (the same IAssertionHelper AnyUnit's
/// own reflection-based test invocation constructs and passes to a
/// discovered [Test] method) to a result value. Reader-shaped on
/// purpose: it's the simplest thing that lets a test body reach
/// `helper.Assert`/`helper.Log` without needing an instance of some
/// fixture class to hang them off of.
type TestM<'a> = IAssertionHelper -> 'a

/// A single, already-built AnyUnit test case - a name plus a thunk that
/// runs the test body. Built directly as a value (via the `test { }`
/// computation expression below), not discovered via reflection over a
/// [Test]-attributed INSTANCE method on a class the way every other
/// AnyUnit style (NUnit/xUnit/FsUnit) works. That reflection path
/// can't see F#'s usual top-level `let testName () = ...` style at all:
/// it compiles to a STATIC method, and AnyUnit's own fixture/method
/// discovery (Utility.AllMethods, used by every TestFixtureDiscoveryAttributeBase)
/// hardcodes BindingFlags.Instance throughout - confirmed directly by
/// testing it. Building Test values by hand sidesteps that reflection
/// path entirely; see Runner.fs for how they get executed and folded
/// into AnyUnit's normal Result/ResultsFile output.
type Test =
    { Name: string
      Run: TestM<unit> }

/// Read-only access to this test's IAssert, for calling straight into
/// whichever style package's IAssert extension methods you want (e.g.
/// AnyUnit.Style.Xunit's `assert'.Equal(x, y)`, or AnyUnit.Constraints'
/// `assert'.That(...)`) - AnyUnit.Style.FSharp doesn't provide its own
/// assertion vocabulary, it only wires up test *values* to AnyUnit's
/// existing Assert/Result/Log machinery. Bind it with `let!`:
///
///     let myTest = test "name" {
///         let! assert' = assertion
///         assert'.Equal(1, 1)
///     }
let assertion: TestM<IAssert> = fun helper -> helper.Assert

/// Same idea for the test's log (Console.WriteLine-equivalent output
/// captured into the Result, same as every other style).
let log: TestM<ILog> = fun helper -> helper.Log

/// `test "name" { ... }` - ordinary let!/do!/for/while/try, plus
/// use!/use for "start/stop": binding a disposable resource via
/// use!/use runs the rest of the body inside a `use`, so Dispose is the
/// "stop" half with no explicit teardown code needed at the call site.
/// Any producer function meant to be used with let!/use! (e.g. a
/// `tesseractEngine name : TestM<TesseractEngine>` helper) should
/// itself return a TestM<'a>, the same as with any other F# computation
/// expression - a plain `use x = someDisposable` (no `!`) works with an
/// ordinary, non-TestM value too, since it goes through Using directly.
type TestBuilder(name: string) =
    member _.Return(x: 'a) : TestM<'a> = fun _ -> x
    member _.ReturnFrom(m: TestM<'a>) : TestM<'a> = m
    member _.Zero() : TestM<unit> = fun _ -> ()
    member _.Delay(f: unit -> TestM<'a>) : TestM<'a> = fun helper -> (f ()) helper
    member _.Bind(m: TestM<'a>, f: 'a -> TestM<'b>) : TestM<'b> = fun helper -> (f (m helper)) helper
    member _.Combine(a: TestM<unit>, b: TestM<'a>) : TestM<'a> = fun helper -> a helper; b helper
    member _.For(items: 'a seq, body: 'a -> TestM<unit>) : TestM<unit> = fun helper -> for item in items do (body item) helper
    member _.While(guard: unit -> bool, body: TestM<unit>) : TestM<unit> = fun helper -> while guard () do body helper
    member _.TryWith(body: TestM<'a>, handler: exn -> TestM<'a>) : TestM<'a> =
        fun helper -> try body helper with ex -> (handler ex) helper
    member _.TryFinally(body: TestM<'a>, compensation: unit -> unit) : TestM<'a> =
        fun helper -> try body helper finally compensation ()
    member this.Using(resource: 'a, body: 'a -> TestM<'b>) : TestM<'b> when 'a :> IDisposable =
        this.TryFinally(body resource, fun () -> match box resource with null -> () | _ -> resource.Dispose())
    member _.Run(m: TestM<unit>) : Test = { Name = name; Run = m }

/// See TestBuilder's own comment.
let test name = TestBuilder(name)
