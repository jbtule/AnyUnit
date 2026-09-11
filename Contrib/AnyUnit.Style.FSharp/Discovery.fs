module AnyUnit.Style.FSharp.Discovery

open System
open System.Collections.Generic
open System.Reflection
open AnyUnit
open AnyUnit.Run
open AnyUnit.Run.Attributes
open AnyUnit.Util
open AnyUnit.Style.FSharp.Test

// An attribute on an F# top-level `let` VALUE binding (as opposed to a
// function) lands on the compiler-generated PROPERTY, not the
// reflectable getter MethodInfo - confirmed directly (a throwaway repro:
// GetCustomAttributes() on the getter method came back empty; the same
// call on the PropertyInfo had it). Since every other AnyUnit style's
// discovery is built around Utility.GetTopMostCustomAttribute<T> on a
// MethodInfo, that path can't see an attribute placed on a `let value =
// test "name" { ... }` binding at all - which is exactly the shape this
// style is for in the first place. So there's nothing to attribute
// here: a qualifying test is identified purely by its property's TYPE
// (Test) instead, the same way "the value IS the test" already works
// throughout Test.fs/Runner.fs.

let private testProperties (t: Type) =
    t.GetProperties(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
    |> Seq.filter (fun p -> p.PropertyType = typeof<Test> && p.GetIndexParameters().Length = 0 && p.GetGetMethod() <> null)

/// No per-fixture metadata to report (there's no real class-level
/// attribute for it to come from - a qualifying type is just "some
/// module with Test-typed properties on it") - AnyUnit.Run.Fixture's
/// own FixtureInit default already handles a static type correctly on
/// its own (returns null - no instance needed - so TestInvoke's
/// `method.Invoke(null, args)` below, a static call, already works).
type private FSharpFixtureAttribute() =
    inherit TestFixtureAttributeBase()
    override _.GetCategories(_: Type) : IList<string> = upcast List<string>()
    override _.GetDescription(_: Type) : string = String.Empty

/// The de facto [Test]-equivalent for this style: never actually
/// applied via an F# attribute anywhere (see this file's own top
/// comment for why) - FSharpFixture constructs one directly, shared
/// across every discovered property. TestInvoke calls the property's
/// getter to obtain the already-built Test value, then runs its thunk
/// against the SAME IAssertionHelper AnyUnit's own invocation pipeline
/// already constructed - so assert-count tracking, log capture, and
/// exception -> ResultKind classification (Ignore/Fail/Error/Success/
/// NoError) all come from the normal AnyUnit.Run.Test machinery,
/// unchanged.
type private FSharpTestAttribute() =
    inherit TestAttributeBase()
    override _.TestInvoke =
        TestInvoker(fun helper method target args ->
            let value = method.Invoke(target, args) :?> Test
            value.Run helper
            null)
    override _.GetTimeout(_: MethodInfo) = Threading.Timeout.Infinite
    override _.GetCategories(_: MethodInfo) : IList<string> = upcast List<string>()
    override _.GetDescription(_: MethodInfo) : string = String.Empty

/// A Fixture whose harnesses come from static PROPERTIES of type Test
/// (an F# module's `let myTest = test "name" { ... }` bindings), not
/// [Test]-attributed methods - see FSharpTestAttribute's own comment
/// for why GetHarnesses (AnyUnit.Run.Fixture's own default implementation,
/// method-attribute-based) has to be overridden here instead of reused.
type private FSharpFixture(type_: Type) =
    inherit Fixture(FSharpFixtureAttribute(), type_)
    static let sharedAttribute = FSharpTestAttribute() :> TestAttributeBase
    override _.GetHarnesses() : TestHarness seq =
        testProperties type_
        |> Seq.map (fun p -> TestHarness(sharedAttribute, p.GetGetMethod()))

/// `[<assembly: FSharpStyle>]` - AnyUnit.Style.FSharp's discovery opt-in,
/// same shape as AnyUnit.Style.Xunit's XunitStyleAttribute: without it,
/// AnyUnit's default discovery (DefaultDiscovery in Runner.cs) requires
/// an explicit class-level TestFixtureAttributeBase attribute, which
/// this style has no equivalent of at all.
[<AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)>]
type FSharpStyleAttribute() =
    inherit TestFixtureDiscoveryAttributeBase()
    override _.Generator =
        FixtureGenerator(fun a ->
            a.AllTypes()
            |> Seq.filter (fun t -> testProperties t |> Seq.isEmpty |> not)
            |> Seq.map (fun t -> FSharpFixture(t) :> Fixture))
