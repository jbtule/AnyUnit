//
//  Copyright 2013 AnyUnit Contributors
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

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

/// A single, already-built AnyUnit test case - just a thunk that runs
/// the test body. No name of its own: unlike Runner.fs's very first
/// version, which carried a separate description string, the name a
/// test is reported/discovered under is always its own F# binding name
/// (`let passingTest = test { ... }` - Discovery.fs finds it as a
/// property named "passingTest", same as any other AnyUnit style's
/// test method name) - a second, independent name string would only
/// ever say the same thing twice, so there's nothing to carry here.
///
/// Built directly as a value (via the `test { }` computation
/// expression below), not discovered via reflection over a [Test]-
/// attributed INSTANCE method on a class the way every other AnyUnit
/// style (NUnit/xUnit/FsUnit) works. That reflection path can't see
/// F#'s usual top-level `let testName () = ...` style at all: it
/// compiles to a STATIC method, and AnyUnit's own fixture/method
/// discovery (Utility.AllMethods, used by every
/// TestFixtureDiscoveryAttributeBase) hardcodes BindingFlags.Instance
/// throughout - confirmed directly by testing it. Building Test values
/// by hand sidesteps that reflection path entirely; see Discovery.fs
/// for how they're found, and Runner.fs for how a list of them gets
/// executed and folded into AnyUnit's normal Result/ResultsFile output.
type Test = { Run: TestM<unit> }

/// Read-only access to this test's IAssert, for calling straight into
/// whichever style package's IAssert extension methods you want (e.g.
/// AnyUnit.Style.Xunit's `Assert.Equal(x, y)`, or AnyUnit.Constraints'
/// `Assert.That(...)`) - AnyUnit.Style.FSharp doesn't provide its own
/// assertion vocabulary, it only wires up test *values* to AnyUnit's
/// existing Assert/Result/Log machinery. Bind it with `let!`, naming it
/// `Assert` (capital A - only the lowercase `assert` keyword is
/// reserved in F#) reads closest to how every other style calls it:
///
///     let myTest = test {
///         let! Assert = assertion
///         Assert.Equal(1, 1)
///     }
let assertion: TestM<IAssert> = fun helper -> helper.Assert

/// Same idea for the test's log (Console.WriteLine-equivalent output
/// captured into the Result, same as every other style).
let log: TestM<ILog> = fun helper -> helper.Log

/// `test { ... }` - ordinary let!/do!/for/while/try, plus use!/use for
/// "start/stop": binding a disposable resource via use!/use runs the
/// rest of the body inside a `use`, so Dispose is the "stop" half with
/// no explicit teardown code needed at the call site. Any producer
/// function meant to be used with let!/use! (e.g. a
/// `tesseractEngine name : TestM<TesseractEngine>` helper) should
/// itself return a TestM<'a>, the same as with any other F# computation
/// expression - a plain `use x = someDisposable` (no `!`) works with an
/// ordinary, non-TestM value too, since it goes through Using directly.
type TestBuilder() =
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
    member _.Run(m: TestM<unit>) : Test = { Run = m }

/// See TestBuilder's own comment.
let test = TestBuilder()
