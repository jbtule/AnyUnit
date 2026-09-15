//
//  Copyright 2026 AnyUnit Contributors
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

namespace AnyUnit.Style.Expecto

open System
open System.Collections.Generic
open System.Threading
open AnyUnit
open AnyUnit.Run

/// The IAssert the test currently running should assert through.
///
/// Ambient on purpose. Expecto's assertions are plain functions -
/// `Expect.equal actual expected "message"` - and preserving that exact
/// call site is the entire reason this style exists; threading an
/// IAssert through every one of them would change every signature and
/// defeat the point.
///
/// The obvious alternative, AnyUnit.Run.Assert.GlobalStyle, is wrong
/// twice: it hands back a THROWAWAY Assert whose AssertCount never
/// reaches the running test's helper (which is exactly why it is
/// [Obsolete]), and setting its `_globalStyleUsed` static degrades
/// pass/no-assert reporting for every other style sharing the process.
///
/// AsyncLocal rather than ThreadStatic because it flows across an await -
/// `testCaseAsync` resumes on whatever thread the continuation lands on,
/// and a ThreadStatic would simply be gone there. Set and cleared around
/// each test by Discovery's TestInvoke; the engine runs tests
/// sequentially, so there is never more than one live at a time.
module internal Ambient =
    // The whole IAssertionHelper, not just its IAssert: Expect needs the
    // assert, and the Logging shim needs the log, and both belong to the
    // same running test.
    let private current = AsyncLocal<IAssertionHelper>()

    let set (helper: IAssertionHelper) = current.Value <- helper
    let clear () = current.Value <- null

    let get () =
        match current.Value with
        | null ->
            raise (InvalidOperationException(
                    "Expect was called outside a running AnyUnit test. The Expect functions "
                    + "assert through the test's own IAssert, which only exists while a test "
                    + "is executing - calling them from module initialisation or from a "
                    + "helper invoked outside a test body cannot work."))
        | helper -> helper.Assert

    /// The running test's log, or None outside a test. Unlike `get` this
    /// does not raise: logging from module initialisation is legitimate
    /// (Expecto suites routinely create a logger at the top of a file),
    /// and the caller falls back to the console there.
    let tryLog () =
        match current.Value with
        | null -> None
        | helper -> Some helper.Log

/// Expecto's assertion vocabulary.
///
/// Argument order is Expecto's, not NUnit's or xUnit's: **actual, then
/// expected, then the message**, and the message is REQUIRED. Every
/// Expecto call site depends on exactly that shape, so it is preserved
/// literally even where it reads oddly to someone arriving from another
/// framework.
[<RequireQualifiedAccess>]
module Expect =

    let private fail (message: string) =
        Ambient.get().Fail(message)

    let private ok () =
        Ambient.get().Okay()

    /// `Expect.equal actual expected message`
    let equal (actual: 'a) (expected: 'a) (message: string) =
        if Object.Equals(box actual, box expected) then ok ()
        else fail (sprintf "%s. Expected %A but got %A." message expected actual)

    let notEqual (actual: 'a) (expected: 'a) (message: string) =
        if Object.Equals(box actual, box expected) then
            fail (sprintf "%s. Expected a value other than %A." message expected)
        else ok ()

    let isTrue (actual: bool) (message: string) =
        if actual then ok () else fail (sprintf "%s. Expected true." message)

    let isFalse (actual: bool) (message: string) =
        if actual then fail (sprintf "%s. Expected false." message) else ok ()

    // Object.ReferenceEquals, not F#'s `isNull`: this module defines its
    // own isNull (Expecto's name, which has to be kept), and inside the
    // module that definition shadows the builtin - so `isNull actual` here
    // would be a recursive call, not a null test.
    let private isNullRef (value: obj) = Object.ReferenceEquals(value, null)

    let isNull (actual: obj) (message: string) =
        if isNullRef actual then ok () else fail (sprintf "%s. Expected null but got %A." message actual)

    let isNotNull (actual: obj) (message: string) =
        if isNullRef actual then fail (sprintf "%s. Expected a value, got null." message) else ok ()

    let isSome (actual: 'a option) (message: string) =
        match actual with
        | Some _ -> ok ()
        | None -> fail (sprintf "%s. Expected Some, got None." message)

    let isNone (actual: 'a option) (message: string) =
        match actual with
        | None -> ok ()
        | Some v -> fail (sprintf "%s. Expected None, got Some %A." message v)

    let isOk (actual: Result<'a, 'b>) (message: string) =
        match actual with
        | Ok _ -> ok ()
        | Error e -> fail (sprintf "%s. Expected Ok, got Error %A." message e)

    let isError (actual: Result<'a, 'b>) (message: string) =
        match actual with
        | Error _ -> ok ()
        | Ok v -> fail (sprintf "%s. Expected Error, got Ok %A." message v)

    let isEmpty (actual: seq<'a>) (message: string) =
        if Seq.isEmpty actual then ok ()
        else fail (sprintf "%s. Expected an empty sequence." message)

    let isGreaterThan (actual: 'a) (expected: 'a) (message: string) =
        if compare actual expected > 0 then ok ()
        else fail (sprintf "%s. Expected %A to be greater than %A." message actual expected)

    let isLessThan (actual: 'a) (expected: 'a) (message: string) =
        if compare actual expected < 0 then ok ()
        else fail (sprintf "%s. Expected %A to be less than %A." message actual expected)

    let stringContains (actual: string) (substring: string) (message: string) =
        if not (isNullRef actual) && actual.Contains(substring) then ok ()
        else fail (sprintf "%s. Expected %A to contain %A." message actual substring)

    let sequenceEqual (actual: seq<'a>) (expected: seq<'a>) (message: string) =
        let a = List.ofSeq actual
        let e = List.ofSeq expected
        if a = e then ok ()
        else fail (sprintf "%s. Expected %A but got %A." message e a)

    /// `Expect.contains actual element message` - the sequence holds the element.
    /// Found missing by porting a real Expecto suite (cwtools), along with
    /// the three below it.
    let contains (actual: seq<'a>) (element: 'a) (message: string) =
        if actual |> Seq.exists (fun x -> x = element) then ok ()
        else fail (sprintf "%s. Expected the sequence to contain %A." message element)

    let isNonEmpty (actual: seq<'a>) (message: string) =
        if Seq.isEmpty actual then fail (sprintf "%s. Expected a non-empty sequence." message)
        else ok ()

    /// `Expect.hasLength actual expected message` - exact element count.
    let hasLength (actual: seq<'a>) (expected: int) (message: string) =
        let n = Seq.length actual
        if n = expected then ok ()
        else fail (sprintf "%s. Expected length %d but got %d." message expected n)

    /// `Expect.hasCountOf actual expected selector message` - exactly
    /// `expected` elements satisfy `selector`. Expecto's own signature
    /// takes the count as uint32, which is preserved so `10u` at a real
    /// call site keeps compiling.
    let hasCountOf (actual: seq<'a>) (expected: uint32) (selector: 'a -> bool) (message: string) =
        let n = actual |> Seq.filter selector |> Seq.length |> uint32
        if n = expected then ok ()
        else fail (sprintf "%s. Expected %d matching elements but found %d." message expected n)

    /// Every element satisfies the predicate.
    let all (actual: seq<'a>) (predicate: 'a -> bool) (message: string) =
        match actual |> Seq.tryFind (predicate >> not) with
        | None -> ok ()
        | Some bad -> fail (sprintf "%s. %A did not satisfy the predicate." message bad)

    /// `Expect.throws f message` - f must raise something.
    let throws (f: unit -> unit) (message: string) =
        let raised =
            try
                f ()
                false
            with
            // An assertion failure or an Assert.Ignore from INSIDE f is
            // this test's own verdict, not the exception f was supposed to
            // throw - swallowing it would turn a genuine failure into a
            // pass. Let both out untouched.
            | :? AssertionException -> reraise ()
            | :? IgnoreException -> reraise ()
            | _ -> true
        if raised then ok () else fail (sprintf "%s. Expected an exception to be raised." message)

    /// `Expect.throwsT<'ex> f message` - f must raise exactly 'ex (or a subtype).
    let throwsT<'ex when 'ex :> exn> (f: unit -> unit) (message: string) =
        let outcome =
            try
                f ()
                Choice1Of2 ()
            with
            | :? AssertionException -> reraise ()
            | :? IgnoreException -> reraise ()
            | ex -> Choice2Of2 ex
        match outcome with
        | Choice1Of2 () -> fail (sprintf "%s. Expected %s to be raised, but nothing was." message typeof<'ex>.Name)
        | Choice2Of2 ex when (ex :? 'ex) -> ok ()
        | Choice2Of2 ex ->
            fail (sprintf "%s. Expected %s but got %s: %s" message typeof<'ex>.Name (ex.GetType().Name) ex.Message)

    /// Unconditional failure, matching Expecto's own `failtest`.
    let failtest (message: string) : unit = fail message
