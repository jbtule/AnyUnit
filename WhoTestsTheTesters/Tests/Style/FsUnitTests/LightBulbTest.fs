//Tests adapted from FSUnit examples https://github.com/fsharp/FsUnit
namespace FsUnitTests

open PclUnit
open PclUnit.Style.FsUnit

type LightBulb(state) =
    member x.On = state
    override x.ToString() =
        match x.On with
        | true  -> "On"
        | false -> "Off"

[<TestFixture>] 
type ``Given a LightBulb that has had its state set to true`` ()=
    inherit AssertionHelper ()
    let lightBulb = new LightBulb(true)

    [<Test>] member this.
     ``when I ask whether it is On it answers true._Success`` ()=
            lightBulb.On |> this.should be True

    [<Test>] member this.
     ``when I convert it to a string it becomes "On"._Success`` ()=
            string lightBulb |> this.should equal "On"

[<TestFixture>]
type ``Given a LightBulb that has had its state set to false`` ()=
    inherit AssertionHelper ()
    let lightBulb = new LightBulb(false)

    [<Test>] member this.
     ``when I ask whether it is On it answers false._Success`` ()=
            lightBulb.On |> this.should be False

    [<Test>] member this.
     ``when I convert it to a string it becomes "Off"._Success`` ()=
            string lightBulb |> this.should equal "Off"