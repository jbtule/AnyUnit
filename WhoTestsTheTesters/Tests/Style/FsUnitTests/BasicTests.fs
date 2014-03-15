//Tests adapted from FSUnit examples https://github.com/fsharp/FsUnit
namespace FsUnitTests

open PclUnit
open PclUnit.Style.FsUnit

[<TestFixture>]
type BasicTests () =
    inherit AssertionHelper ()

    [<Test>]
    member this.``One object equals or does not equal another _Success`` ()=
        1 |> this.should equal 1
        1 |> this.should not' (equal 2)

    [<Test>]
    member this.``One object equals another _Fail`` ()=
        1 |> this.should equal 2
    
    [<Test>]
    member this.``One object does not equals another _Fail`` ()=
         1 |> this.should not' (equal 1)

    [<Test>]
    member this.``One numeric object equals or does not equal another, with a specified tolerance: _Success`` ()=
        10.1 |> this.should (equalWithin 0.1) 10.11
        10.1 |> this.should not' ((equalWithin 0.001) 10.11)

    [<Test>]
    member this.``A string does or does not start with or end with a specified substring: _Success`` ()=
        "ships" |> this.should startWith "sh"
        "ships" |> this.should not' (startWith "ss")
        "ships" |> this.should endWith "ps"
        "ships" |> this.should not' (endWith "ss")

    [<Test>]
    member this.``A List, Seq, or Array instance contains or does not contain a value _Success`` ()=
        [1] |> this.should contain 1
        [] |> this.should not' (contain 1)

    [<Test>]
    member this.``An Array instance has a certain length _Success `` () =
        seq { 1..4 } |> System.Linq.Enumerable.ToArray |> this.should haveLength 4

    [<Test>]
    member this.``A Collection instance has a certain count _Success`` () =
        ResizeArray([|1;2;3;4|]) |> this.should haveCount 4

    [<Test>]
    member this.``A function should throw a certain type of exception: _Success`` ()=
        (fun () -> failwith "BOOM!" |> ignore) |> this.should throw typeof<System.Exception>

    [<Test>]
    member this.``A function should throw a certain type of exception: _Fail`` ()=
        (fun () -> failwith "BOOM!" |> ignore) |> this.should throw typeof<System.NotImplementedException>

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
        [] |> this.should be Empty 

    [<Test>]
    member this.TestNotEmpty_Success () =
        [1] |> this.should not' (be Empty) 

    [<Test>]
    member this.TestInstanceOf_Success () =
        "test" |> this.should be instanceOfType<string> 

    [<Test>]
    member this.TestNotInstanceOf_Success () =
        "test" |> this.should not' (be instanceOfType<int>) 
   
    [<Test>]
    member this.TestNan_Success () =
        2.0 |> this.should not' (be NaN) 

    [<Test>]
    member this.TestUnique_Success () =
        [1;2;3] |> this.should be unique
