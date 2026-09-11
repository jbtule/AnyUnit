namespace AnyUnit.Style.FsUnit

open AnyUnit.Constraints.Pieces

type AssertionHelper () = 
    inherit AnyUnit.AssertionHelper ()
    static do
        AnyUnit.Constraints.AssertEx.ExcludeFromStack.Add(sprintf "at %s." typeof<AssertionHelper>.FullName)
        AnyUnit.Constraints.AssertEx.ExcludeFromStack.Add("at AnyUnit.Style.FsUnit.TopLevelOperators.")
    member this.should (f : 'a -> #Constraint) x (y : obj) = shouldHelper (this.Assert) f x y
    member this.shouldFail (f : unit -> unit) = shouldFailHelper (this.Assert) f
    member this.printf fmt = Printf.ksprintf (fun s -> this.Log.Write(s)) fmt
    member this.printfn fmt = Printf.ksprintf (fun s -> this.Log.WriteLine(s)) fmt

type TestClass = AssertionHelper