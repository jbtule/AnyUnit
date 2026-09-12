/// Real F# entry point calling AnyUnit.TestingPlatform.Runner.RunAsync
/// directly - now that it's a real, compiled method (not MSBuild-generated
/// C# text), an F# project doesn't need EnableAnyUnitRunner's own C#-only
/// codegen (or the separate C#-shell satellite that used to work around
/// that) at all. FSharpTests itself stays a plain library assembly (no
/// OutputType=Exe/MTP package reference of its own) - this project is
/// still the actual MTP host, just a real F# one now, referencing it by
/// simple assembly name the same way <AnyUnitTestAssembly> used to.
module FSharpTests.Mtp.Program

open System.Reflection

[<EntryPoint>]
let main args =
    AnyUnit.TestingPlatform.Runner.RunAsync(args, Assembly.Load("FSharpTests")).GetAwaiter().GetResult()
