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

module AnyUnit.Style.Expecto.Discovery

open System
open System.Collections.Generic
open System.Reflection
open AnyUnit
open AnyUnit.Run
open AnyUnit.Run.Attributes
open AnyUnit.Util

/// This style's own test tree. Aliased because `open AnyUnit.Run` above
/// brings in AnyUnit.Run.Test - the ENGINE's runnable test - and the two
/// names would otherwise collide on every annotation below.
type private Tree = AnyUnit.Style.Expecto.Test

/// One runnable leaf pulled out of a test tree: where it sits, what it
/// does, and whether it was marked pending anywhere on the way down.
type private Leaf =
    { Path: string list
      Code: TestCode
      Pending: bool
      Requires: TestCapabilities }

/// Flattens the tree. `pending` accumulates downward, so ptestList marks
/// everything beneath it however deeply nested.
let rec private flatten (prefix: string list) (pending: bool) (requires: TestCapabilities) (test: Tree) : Leaf list =
    match test with
    | TestCase (name, code, casePending) ->
        [ { Path = List.rev (name :: prefix)
            Code = code
            Pending = pending || casePending
            Requires = requires } ]
    | TestList (name, tests, listPending) ->
        tests |> List.collect (flatten (name :: prefix) (pending || listPending) requires)
    // Accumulates downward, like pending: a requirement declared over a
    // list applies to every leaf under it. Adds no path segment - it
    // describes the tests, it is not one of them.
    | TestRequires (capability, inner) ->
        flatten prefix pending (requires ||| capability) inner

/// Expecto's own reporting joins a test's ancestry with '/', so a ported
/// suite's names look the same here as they did there.
let private joinPath (path: string list) = String.Join("/", path)

/// No per-fixture metadata to report: a fixture here is one [<Tests>]
/// binding, not a class anyone attributed. FixtureInit's default already
/// handles a static type correctly (returns null - no instance needed),
/// which is what the module holding the binding is.
type private ExpectoFixtureAttribute() =
    inherit TestFixtureAttributeBase()
    override _.GetCategories(_: Type) : IList<string> = upcast List<string>()
    override _.GetDescription(_: Type) : string = String.Empty

/// The [Test]-equivalent, one instance per leaf, closing over that leaf's
/// own body. Never applied as an F# attribute anywhere - constructed
/// directly, the same way AnyUnit.Style.FSharp's own does, because the
/// thing being described is a value in a list rather than a method anyone
/// could attribute.
type private ExpectoTestAttribute(leaf: Leaf) =
    inherit TestAttributeBase()

    override _.TestInvoke =
        TestInvoker(fun helper _method _target _args ->
            if leaf.Pending then
                raise (IgnoreException("Pending (ptest)"))

            // The ambient IAssert for exactly this test's duration - see
            // Ambient's own comment for why this is how Expect reaches it,
            // and why Assert.GlobalStyle is not.
            Ambient.set helper
            try
                match leaf.Code with
                | Sync body ->
                    body ()
                    null
                // Handed back rather than waited on here: AnyUnit.Run.
                // AsyncTestResult owns that decision, including refusing to
                // block on a runtime that can never resume (browser-wasm).
                // StartImmediateAsTask, not StartAsTask - it runs on THIS
                // thread until a real suspension, so a workflow that never
                // suspends comes back already completed and works there too.
                | AsyncCode body -> box (Async.StartImmediateAsTask body)
                | TaskCode body -> box (body ())
            finally
                Ambient.clear ())

    override _.GetTimeout(_: MethodInfo) = Threading.Timeout.Infinite
    override _.GetCategories(_: MethodInfo) : IList<string> = upcast List<string>()
    override _.GetDescription(_: MethodInfo) : string = String.Empty

/// A fixture per [<Tests>] binding rather than per module: the binding's
/// root testList names it, and each leaf keeps the remaining path as its
/// own name. `testList "math" [ testList "addition" [ testCase "negatives" ] ]`
/// therefore reports fixture "math", test "addition/negatives" - which is
/// how Expecto itself prints it.
type private ExpectoFixture(type_: Type, name: string, getter: MethodInfo, leaves: Leaf list) =
    inherit Fixture(ExpectoFixtureAttribute(), type_)

    do
        // FixtureMeta's constructor named this after the TYPE, which would
        // collapse every binding in a module onto one fixture. Both halves
        // are reset: UniqueName is what ResultsFile.Add keys on, so leaving
        // it type-derived would merge two bindings' results together.
        base.Name <- name
        base.UniqueName <- sprintf "T:%s.%s" type_.FullName name

    override _.GetHarnesses() : TestHarness seq =
        leaves
        |> Seq.map (fun leaf ->
            // The getter is passed only so the harness has a MethodInfo to
            // hang off (declaring type, assembly); it is never invoked -
            // the leaf's body is already closed over by the attribute. The
            // DisplayName is what actually distinguishes these, since every
            // leaf of one binding shares that single getter.
            TestHarness(ExpectoTestAttribute(leaf) :> TestAttributeBase, getter,
                        DisplayName = joinPath leaf.Path,
                        RequiredCapabilities = leaf.Requires))

/// Static properties of type Test carrying [<Tests>].
///
/// PropertyInfo, not MethodInfo: an attribute on an F# `let` VALUE binding
/// lands on the compiler-generated property, and is simply absent from the
/// getter - confirmed directly, and documented at length in
/// AnyUnit.Style.FSharp's Discovery.fs for the same reason.
let private testProperties (t: Type) =
    t.GetProperties(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
    |> Seq.filter (fun p ->
        p.PropertyType = typeof<Tree>
        && p.GetIndexParameters().Length = 0
        && p.GetGetMethod() <> null
        && p.GetCustomAttributes(typeof<TestsAttribute>, true).Length > 0)

let private fixturesFor (t: Type) : Fixture seq =
    testProperties t
    |> Seq.choose (fun property ->
        // Evaluating the binding here runs the user's own test-tree
        // construction at DISCOVERY time. That is inherent to a
        // value-based style - the leaves cannot be counted or named
        // without it - and is exactly what Expecto itself does.
        // An F# union does not admit null as a proper value, so the
        // uninitialised case is tested on the boxed object before the cast
        // rather than pattern-matched afterwards. It is reachable: a `let`
        // binding whose initialiser has not run yet reads as null.
        let value = property.GetValue(null, null)
        if Object.ReferenceEquals(value, null) then None
        else
            let root = value :?> Tree
            let name, leaves =
                match root with
                | TestList (name, tests, pending) ->
                    name, tests |> List.collect (flatten [] pending TestCapabilities.None)
                // A bare testCase, or a requires wrapping one, has no list
                // to name the fixture, so the binding's own name serves.
                | _ -> property.Name, flatten [] false TestCapabilities.None root
            if List.isEmpty leaves then None
            else Some(ExpectoFixture(t, name, property.GetGetMethod(), leaves) :> Fixture))

/// `[<assembly: ExpectoStyle>]` - this style's discovery opt-in, the same
/// shape as AnyUnit.Style.FSharp's and AnyUnit.Style.Xunit's. Without it
/// AnyUnit's default discovery looks for a class-level
/// TestFixtureAttributeBase, which a module of `let` bindings has no
/// equivalent of.
[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)>]
type ExpectoStyleAttribute() =
    inherit TestFixtureDiscoveryAttributeBase()
    override _.Generator =
        FixtureGenerator(fun assembly -> assembly.AllTypes() |> Seq.collect fixturesFor)
