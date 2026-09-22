/// Pins the reason AnyUnit.Style.Core exists (#66).
///
/// F# has no CS0104. When two opened namespaces export the same simple
/// name the LAST `open` wins, silently. Before #66 the core's built-in
/// style lived in the AnyUnit root and exported `TestAttribute` /
/// `TestFixtureAttribute` under exactly the names AnyUnit.Style.Nunit
/// exports, so an F# file that opened `AnyUnit` after the style got the
/// CORE's attributes with no error and no warning anywhere.
///
/// That is not cosmetic. `[<SetUp>]`, `[<TearDown>]`, `[<Ignore>]`,
/// `[<Platform>]` and ITestAction are dispatched by
/// AnyUnit.Style.Nunit.TestAttribute.TestInvoke, and constructor args /
/// `[<OneTimeSetUp>]` / `[<OneTimeTearDown>]` by that style's
/// TestFixtureAttribute; the core's attributes override none of it. So
/// the silent swap silently stopped a fixture's setup and teardown from
/// running. Verified by execution before the fix: this exact fixture
/// reported Fail, with SetUp never having run.
///
/// The opens below are deliberately in the HAZARD order - the style
/// first, `AnyUnit` last. That is what made it break. It is safe now
/// only because `AnyUnit` no longer exports an attribute of either name;
/// put TestAttribute or TestFixtureAttribute back in the root and this
/// file goes red rather than going quiet.
module ComboTests.FSharp.OpenOrder

open AnyUnit.Style.Nunit
open AnyUnit

type private Ran() =
    static member val SetUp = false with get, set
    static member val OneTimeSetUp = false with get, set

[<TestFixture>]
type OpenOrderHazard() =
    inherit AnyUnit.Style.Nunit.AssertionHelper()

    [<OneTimeSetUp>]
    member _.Once() = Ran.OneTimeSetUp <- true

    [<SetUp>]
    member _.Before() = Ran.SetUp <- true

    /// Fails if `[<Test>]`/`[<TestFixture>]` resolved to the core's
    /// attributes instead of this style's - the core's TestInvoke does
    /// not run [<SetUp>] at all.
    [<Test>]
    member this.setUpRanUnderHazardOpenOrder_Success() =
        this.Assert.True(Ran.SetUp, "[<SetUp>] did not run - `open AnyUnit` shadowed AnyUnit.Style.Nunit's [<Test>]")

    /// Same for the fixture attribute, whose OneTimeSetUp override is the
    /// style's, not the core's.
    [<Test>]
    member this.oneTimeSetUpRanUnderHazardOpenOrder_Success() =
        this.Assert.True(Ran.OneTimeSetUp, "[<OneTimeSetUp>] did not run - `open AnyUnit` shadowed AnyUnit.Style.Nunit's [<TestFixture>]")
