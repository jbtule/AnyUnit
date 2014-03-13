// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.

open System
open Microsoft.FSharp.Compiler.Interactive.Shell
open System.Text.RegularExpressions


[<EntryPoint>]
let main(argv) =
    
    let argAll = Array.append [| "C:\\fsi.exe" |] argv
    let argFix = argAll |> Array.map (fun a -> if a.StartsWith("--eval:") then "--noninteractive" else a)
    let optFind = argv |> Seq.tryFind (fun a -> a.StartsWith "--eval:")
    let evalData = if optFind.IsSome then
                        optFind.Value.Replace("--eval:",String.Empty)
                     else
                        String.Empty
    let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
    let fsiSession = FsiEvaluationSession(fsiConfig, argFix, Console.In, Console.Out, Console.Error) 
    if String.IsNullOrWhiteSpace(evalData) then
        fsiSession.Run()
    else
        System.Diagnostics.Debugger.Launch() |> ignore
        fsiSession.EvalInteraction(evalData)
    0