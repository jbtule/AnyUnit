/// Same shape as FSharpTests.Mtp's own Program.fs - see that file's
/// comment for why this is real F# now instead of a C# satellite shell.
module ComboTests.FSharp.Mtp.Program

open System.Reflection

[<EntryPoint>]
let main args =
    AnyUnit.TestingPlatform.Runner.RunAsync(args, Assembly.Load("ComboTests.FSharp")).GetAwaiter().GetResult()
