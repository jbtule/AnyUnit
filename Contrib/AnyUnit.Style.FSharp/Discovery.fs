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
// here: a qualifying plain test is identified purely by its property's
// TYPE (Test) instead, the same way "the value IS the test" already
// works throughout Test.fs/Runner.fs.

let private testProperties (t: Type) =
    t.GetProperties(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
    |> Seq.filter (fun p -> p.PropertyType = typeof<Test> && p.GetIndexParameters().Length = 0 && p.GetGetMethod() <> null)

/// True once every one of a method's parameters carries its own
/// IArgParameter (NUnit's Values/ValueSource/Random, or any other
/// style's own implementation) - mirrors AnyUnit.Style.Nunit.
/// TestAttribute's own "values.All(v => v.Attr != null)" check: partial
/// coverage doesn't qualify, matching NUnit's own combinatorial
/// semantics.
let private allParametersHaveArgAttribute (m: MethodInfo) =
    let parameters = m.GetParameters()
    parameters.Length > 0
    && parameters |> Array.forall (fun p -> p.GetCustomAttributes(true) |> Seq.exists (fun a -> a :? IArgParameter))

/// A parametrized test is an ordinary F# *function* (`let f (x) = test
/// { ... }`), which - unlike a plain value - takes real parameters and
/// so compiles to a genuine method, not a property. Qualifies as a data
/// test once it carries at least one IRowInlineParameter row attribute
/// or IGeneratingParameter method-level generator, or every one of its
/// parameters carries its own IArgParameter - any style's (xUnit's
/// InlineData/ClassData/PropertyData, NUnit's TestCase/Values/
/// ValueSource/Random, or a user's own implementation), recognized
/// purely via these shared core interfaces with no dependency on either
/// style's assembly. Requiring one of these for qualification (not just
/// "any method returning Test") mirrors how a real [Theory] needs
/// [InlineData]/similar to mean anything.
let private dataTestMethods (t: Type) =
    t.GetFlattenedMethods()
    |> Seq.filter (fun m ->
        m.ReturnType = typeof<Test>
        && not m.IsSpecialName // excludes property getters/setters
        && m.GetParameters().Length > 0
        && (m.GetCustomAttributes(true) |> Seq.exists (fun a -> a :? IRowInlineParameter || a :? IGeneratingParameter)
            || allParametersHaveArgAttribute m))

/// No per-fixture metadata to report (there's no real class-level
/// attribute for it to come from - a qualifying type is just "some
/// module with Test-typed properties/data-test methods on it") -
/// AnyUnit.Run.Fixture's own FixtureInit default already handles a
/// static type correctly on its own (returns null - no instance needed
/// - so TestInvoke's `method.Invoke(target, args)` below, a static
/// call, already works).
type private FSharpFixtureAttribute() =
    inherit TestFixtureAttributeBase()
    override _.GetCategories(_: Type) : IList<string> = upcast List<string>()
    override _.GetDescription(_: Type) : string = String.Empty

/// The de facto [Test]-equivalent for this style's plain (non-data)
/// tests: never actually applied via an F# attribute anywhere (see this
/// file's own top comment for why) - FSharpFixture constructs one
/// directly, shared across every discovered property/method. TestInvoke
/// calls the method (a property getter for a plain test, or a real
/// method for a data test) to obtain the already-built Test value, then
/// runs its thunk against the SAME IAssertionHelper AnyUnit's own
/// invocation pipeline already constructed - so assert-count tracking,
/// log capture, and exception -> ResultKind classification
/// (Ignore/Fail/Error/Success/NoError) all come from the normal
/// AnyUnit.Run.Test machinery, unchanged.
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

/// Cross-product of each parameter's own candidate-value list into full
/// rows - same shape as AnyUnit.Style.Nunit.TestAttribute's own
/// CombineHelper, reimplemented here since Discovery.fs has no
/// dependency on AnyUnit.Style.Nunit to reuse it directly.
let private crossProduct (valueSets: obj list list) : obj list list =
    valueSets |> List.fold (fun acc values -> [ for row in acc do for v in values -> row @ [ v ] ]) [ [] ]

/// A data test's invocation is identical to a plain test's (call the
/// method, run the returned Test's thunk) - only ParameterSets differs,
/// unioning rows from three possible sources, regardless of which style
/// they came from: each IRowInlineParameter attribute's own single row,
/// each IGeneratingParameter attribute's own GetData rows, and - when
/// every parameter carries its own IArgParameter - the cross-product of
/// each parameter's own values. Note the row's own values aren't needed
/// for naming: AnyUnit.Run.Test's constructor already names each row
/// distinctly from its real reflection args.
type private DataTestAttribute() =
    inherit FSharpTestAttribute()
    override _.ParameterSets =
        TestParameterSetProducer(fun method ->
            let rowRows =
                method.GetCustomAttributes(true)
                |> Seq.choose (fun a -> match a with :? IRowInlineParameter as r -> Some r.Arguments | _ -> None)

            let generatingRows =
                method.GetCustomAttributes(true)
                |> Seq.choose (fun a -> match a with :? IGeneratingParameter as g -> Some g | _ -> None)
                |> Seq.collect (fun g -> g.GetData(method, [||]))

            let combinatorialRows =
                if allParametersHaveArgAttribute method then
                    method.GetParameters()
                    |> Array.map (fun p ->
                        p.GetCustomAttributes(true)
                        |> Seq.pick (fun a -> match a with :? IArgParameter as arg -> Some arg | _ -> None)
                        |> fun arg -> arg.GetData(p) |> Seq.cast<obj> |> List.ofSeq)
                    |> List.ofArray
                    |> crossProduct
                    |> Seq.map List.toArray
                else Seq.empty

            Seq.concat [ rowRows; generatingRows; combinatorialRows ]
            |> Seq.map (fun args -> ParameterSet(args)))

/// A Fixture whose harnesses come from static PROPERTIES of type Test
/// (an F# module's `let myTest = test { ... }` bindings) and
/// parametrized data-test METHODS (`let f (x) = test { ... }`, carrying
/// any style's IRowInlineParameter row attribute), not [Test]-attributed
/// instance methods - see FSharpTestAttribute's/dataTestMethods' own
/// comments for why GetHarnesses (AnyUnit.Run.Fixture's own default
/// implementation, method-attribute-based) has to be overridden here
/// instead of reused.
type private FSharpFixture(type_: Type) =
    inherit Fixture(FSharpFixtureAttribute(), type_)
    static let plainAttribute = FSharpTestAttribute() :> TestAttributeBase
    static let dataAttribute = DataTestAttribute() :> TestAttributeBase
    override _.GetHarnesses() : TestHarness seq =
        Seq.append
            (testProperties type_ |> Seq.map (fun p -> TestHarness(plainAttribute, p.GetGetMethod())))
            (dataTestMethods type_ |> Seq.map (fun m -> TestHarness(dataAttribute, m)))

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
            |> Seq.filter (fun t -> not (Seq.isEmpty (testProperties t)) || not (Seq.isEmpty (dataTestMethods t)))
            |> Seq.map (fun t -> FSharpFixture(t) :> Fixture))
