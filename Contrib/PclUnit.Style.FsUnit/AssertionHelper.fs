namespace PclUnit.Style.FsUnit
open PclUnit.Constraints.Pieces

type AssertionHelper () = 
    inherit PclUnit.AssertionHelper ()
    static do
        PclUnit.Constraints.AssertEx.ExcludeFromStack.Add(sprintf "at %s." typeof<AssertionHelper>.FullName)
        PclUnit.Constraints.AssertEx.ExcludeFromStack.Add("at PclUnit.Style.FsUnit.TopLevelOperators.")
    member this.should (f : 'a -> #Constraint) x (y : obj) = shouldHelper (this.Assert) f x y
    member this.shouldFail (f : unit -> unit) = shouldFailHelper (this.Assert) f
    member this.printf fmt = Printf.ksprintf (fun s -> this.Log.Write(s)) fmt
    member this.printfn fmt = Printf.ksprintf (fun s -> this.Log.WriteLine(s)) fmt

type TestClass = AssertionHelper