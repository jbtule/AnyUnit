//Test adapted from FSUnit examples https://github.com/fsharp/FsUnit
module BowlingGame.``A game of bowling``

open AnyUnit
open AnyUnit.Style.Xunit
open AnyUnit.Style.FsUnit

[<assembly: XunitStyle>] 
do ()

let (|EndOfGame|IncompleteStrike|Strike|Normal|Other|) (l, frame) =
    match l with
    | _ when frame = 11            -> EndOfGame(0)
    | [10;s]                       -> IncompleteStrike(10+s+s)
    | 10::s::n::tail               -> Strike(10+s+n, s::n::tail)
    |  f::s::n::tail when f+s = 10 -> Normal(f+s+n,  n::tail)
    |  f::s::n::tail               -> Normal(f+s,    n::tail)
    | ls                           -> Other(List.fold (+) 0 ls)

let scoreBowls bowls =
    let rec scoreBowls' frame l current_score =
        let nextframe = scoreBowls' (frame+1)
        match (l, frame) with
        | EndOfGame(score)        -> current_score + score
        | IncompleteStrike(score) -> current_score + score
        | Strike(score, l)        -> nextframe l (current_score + score)
        | Normal(score, l)        -> nextframe l (current_score + score)
        | Other(score)            -> current_score + score
    scoreBowls' 1 bowls 0

type Fixture () =
    inherit TestClass()

    [<Fact>] 
    member this.``with simple scores should get the expected score. expect:_Success`` () =
        scoreBowls [1;2;3] |> this.should equal 6

    [<Fact>] 
    member this.``with simple scores should get the expected score. expect:_Fail`` () =
        scoreBowls [1;2;3] |> this.should equal 9

    [<Fact>]
    member this.``with a spare should get the expected score (spare). expect:_Success`` () =
        scoreBowls [2;8;1] |> this.should equal 12

    [<Fact>]
    member this.``with a strike should get the expected score (strike). expect:_Success`` () =
        scoreBowls [10;1;2] |> this.should equal 16

    [<Fact>]
    member this.``that is perfect should get a score of 300. expect:_Success``() =
        scoreBowls [for i in 1..18 -> 10] |> this.should equal 300

    [<Fact>]
    member this.``with spares in the last frame should get the expected score (spare in last frame). expect:_Success`` () =
        scoreBowls ([for i in 1..18 -> 0] @ [2;8;1]) |> this.should equal 11

    [<Fact>]
    member this.``with a strike in the last frame should get the expected score (strike in last frame). expect:_Success`` () =
        scoreBowls ([for i in 1..18 -> 0] @ [10;10;1]) |> this.should equal 21

    [<Fact>] 
    member this.``with double strikes should add the score of the first strike to the score of the second. expect:_Success`` () =
        scoreBowls [10;10;1] |> this.should equal 33

    [<Fact>]
    member this.``that looks like an average bowler's game should get the expected score (example game). expect:_Success`` () =
        scoreBowls [1;4;4;5;6;4;5;5;10;0;1;7;3;6;4;10;2;8;6] |> this.should equal 133