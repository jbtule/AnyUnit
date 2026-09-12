/// Covers Discovery.fs's parametrized-test support: an ordinary F#
/// *function* (real parameters, so it compiles to a genuine method, not
/// a property - see Discovery.fs's own top comment) carrying
/// InlineDataAttribute rows. Body is the same `test { }` builder used
/// throughout Basic.fs - no separate CE needed. All rows pass, so each
/// is suffixed _Success per WhoTestsTheTesters/ConventionTestProcessor's
/// substring convention.
module FSharpTests.DataTests

open AnyUnit.Style.FSharp.Test
open AnyUnit.Style.FSharp.Discovery

[<InlineData(1, 1)>]
[<InlineData(2, 4)>]
[<InlineData(3, 9)>]
let square_Success (n: int) (expected: int) = test {
    let! Assert = assertion
    Assert.True(n * n = expected, $"Expected {n}*{n} = {expected}")
}
