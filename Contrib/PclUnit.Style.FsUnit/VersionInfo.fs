namespace System
open System.Reflection

[<assembly: AssemblyVersionAttribute("1.0.5.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0.5.0")>]
[<assembly: AssemblyInformationalVersionAttribute("1.0.5.0 (Built Locally)")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0.5.0"
