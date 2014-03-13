namespace FsUnitTests

open PclUnit
open PclUnit.Style.FsUnit

[<TestFixture>]
type BasicTests () =
    inherit AssertionHelper ()

    [<Test>]
    member this.TestTrue_Success () =
        true |> this.should be True

    [<Test>]
    member this.TestTrue_Fail () =
        false |> this.should be True

    [<Test>]
    member this.TestFalse_Success () =
        false |> this.should not' (be True)
    
    [<Test>]
    member this.TestEmptyStr_Success () =
        "" |> this.should be EmptyString

    [<Test>]
    member this.TestEmptyOrNull_Success () =
        "" |> this.should be NullOrEmptyString

    [<Test>]
    member this.TestNullOrEmpty_Success ()=
        null |> this.should be NullOrEmptyString

    [<Test>]
    member this.TestNull_Success ()=
        null |> this.should be Null
    [<Test>]
    member this.TestNOtNull_Success ()=
        System.Object() |> this.should not' (be Null)
 
    [<Test>]
    member this.TestSame_Success ()=
       let anObj = System.Object()
       anObj |> this.should be (sameAs anObj)

    [<Test>]
    member this.TestNotSame_Success ()=
        let anObj = System.Object()
        let otherObj = System.Object()
        anObj |> this.should not' (be sameAs otherObj)

    [<Test>]
    member this.TestGreaterThan_Success ()=
        11 |> this.should be (greaterThan 10)

    [<Test>]
    member this.TestNotGreaterThan_Success ()=
        9 |> this.should not' (be greaterThan 10)

    [<Test>]
    member this.TestGreaterThanEqual_Success ()=
        11 |> this.should be (greaterThanOrEqualTo 10)

    [<Test>]
    member this.TestNotGreaterThanEqual_Success ()=
        9 |> this.should not' (be greaterThanOrEqualTo 10)
    
    [<Test>]
    member this.TestLessThan_Success ()=
        10 |> this.should be (lessThan 11)

    [<Test>]
    member this.TestNotLessThan_Success ()=
        10 |> this.should not' (be lessThan 9)

    [<Test>]
    member this.TestLessThanEq_Success () =
        10.0 |> this.should be (lessThanOrEqualTo 10.1)

    [<Test>]
    member this.TestNotLessThanEq_Success () =
        10 |> this.should not' (be lessThanOrEqualTo 9)

    [<Test>]
    member this.TestExactType_Success () =
        0.0 |> this.should be ofExactType<float>
         
    [<Test>]
    member this.TestNotExactType_Success () =
        1 |> this.should not' (be ofExactType<obj>)

    [<Test>]
    member this.TestEmpty_Success () =
        [] |> this.should be Empty // NUnit only

    [<Test>]
    member this.TestNotEmpty_Success () =
        [1] |> this.should not' (be Empty) // NUnit only

    [<Test>]
    member this.TestInstanceOf_Success () =
        "test" |> this.should be instanceOfType<string> // Currently, NUnit only and requires version 1.0.1.0+

    [<Test>]
    member this.TestNotInstanceOf_Success () =
        "test" |> this.should not' (be instanceOfType<int>) // Currently, NUnit only and requires version 1.0.1.0+
   
    [<Test>]
    member this.TestNan_Success () =
        2.0 |> this.should not' (be NaN) // Currently, NUnit only and requires version 1.0.1.0+

    [<Test>]
    member this.TestUnique_Success () =
        [1;2;3] |> this.should be unique // Currently, NUnit only and requires version 1.0.1.0+
