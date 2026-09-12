/// Covers AnyUnit.Style.FSharp.Discovery's recognition of the two richer
/// generator interfaces (AnyUnit.Run.Attributes.IGeneratingParameter/
/// IArgParameter), each satisfied here by one other style's own
/// attribute - Discovery.fs itself depends on neither style's assembly,
/// only the shared core interfaces.
module ComboTests.FSharp.RicherGenerators

open AnyUnit.Style.FSharp.Test
open AnyUnit.Style.Xunit
open AnyUnit.Style.Nunit

/// Covers IGeneratingParameter: xUnit's own PropertyDataAttribute
/// reflects a named static property on the declaring type - a
/// parameterless F# `let` VALUE binding (as opposed to a function)
/// compiles to exactly that, a static property, so SquareRowsFromProperty
/// below already satisfies PropertyDataAttribute's own expectations with
/// no special-casing needed.
let SquareRowsFromProperty : seq<obj[]> =
    seq {
        yield [| box 4; box 16 |]
        yield [| box 5; box 25 |]
    }

[<PropertyData("SquareRowsFromProperty")>]
let squareFromProperty_Success (n: int) (expected: int) = test {
    let! Assert = assertion
    Assert.True(n * n = expected, $"Expected {n}*{n} = {expected}")
}

/// Covers IArgParameter: NUnit's own ValuesAttribute on each individual
/// parameter (no method-level data attribute at all) -
/// DataTestAttribute.ParameterSets cross-products each parameter's own
/// values into 2*2 = 4 rows.
let combo_Success ([<Values(1, 2)>] n: int) ([<Values(10, 20)>] m: int) = test {
    let! Assert = assertion
    Assert.True(n > 0 && m > 0, "expected both to be positive")
}
