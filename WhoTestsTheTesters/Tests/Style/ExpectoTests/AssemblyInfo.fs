module ExpectoTests.AssemblyInfo

open AnyUnit.Style.Expecto.Discovery

// Expecto's unit is a VALUE - a testList tree bound with `let` - so there
// is no class-level attribute for AnyUnit's default discovery to find,
// exactly as with AnyUnit.Style.FSharp and AnyUnit.Style.Xunit. This
// assembly-level opt-in is what points discovery at [<Tests>] bindings.
[<assembly: ExpectoStyle>]
do ()
