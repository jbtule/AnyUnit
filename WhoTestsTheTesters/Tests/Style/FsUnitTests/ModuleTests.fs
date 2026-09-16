// Module-level tests with no `this`: the bare `should` reaches the running
// test through AnyUnit.Run.AmbientTest, so its assertions are counted
// against THIS test - which is what makes the _NoError case below
// possible at all. Under the old global-style `should`, one use anywhere
// in the process flipped every assertion-less test in the run to Success.
module FsUnitTests.ModuleTests

open AnyUnit.Style.Xunit
open AnyUnit.Style.FsUnit

[<Fact>]
let ``bare should counts as this test's assertion _Success`` () =
    1 |> should equal 1
    "ships" |> should haveSubstring "hip"

[<Fact>]
let ``bare should failing is this test's failure _Fail`` () =
    1 |> should equal 2

[<Fact>]
let ``no assertion at all is still reported as such _NoError`` () =
    ignore (1 + 1)

[<Fact>]
let ``bare shouldFail _Success`` () =
    shouldFail (fun () -> 1 |> should equal 2)

[<Fact>]
let ``static Assert.Current reaches the running test _Success`` () =
    let ex = AnyUnit.Run.Assert.Current.Throws<System.InvalidOperationException>(fun () -> raise (System.InvalidOperationException "x"))
    ex.Message |> should equal "x"
