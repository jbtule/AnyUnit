/// Covers Discovery.fs's parametrized-test support: an ordinary F#
/// *function* (real parameters, so it compiles to a genuine method, not
/// a property - see Discovery.fs's own top comment) carrying a row
/// attribute that implements AnyUnit.Run.Attributes.IRowInlineParameter.
/// Uses AnyUnit.Style.Xunit's own InlineDataAttribute here (any style's
/// row attribute works identically - Discovery.fs has no dependency on
/// AnyUnit.Style.Xunit itself, only on the shared core interface).
/// Body is the same `test { }` builder used throughout Basic.fs - no
/// separate CE needed. All rows pass, so each is suffixed _Success per
/// WhoTestsTheTesters/ConventionTestProcessor's substring convention.
///
/// Cross-style combo scenarios (IGeneratingParameter/IArgParameter, and
/// tests mixing multiple styles' attributes) live in
/// WhoTestsTheTesters/Tests/Style/ComboTests(.FSharp) instead, keeping
/// this project's own dependencies to a single other style.
module FSharpTests.DataTests

open AnyUnit.Style.FSharp.Test
open AnyUnit.Style.Xunit

[<InlineData(1, 1)>]
[<InlineData(2, 4)>]
[<InlineData(3, 9)>]
let square_Success (n: int) (expected: int) = test {
    let! Assert = assertion
    Assert.True(n * n = expected, $"Expected {n}*{n} = {expected}")
}
