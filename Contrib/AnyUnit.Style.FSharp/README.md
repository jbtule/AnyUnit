# AnyUnit.Style.FSharp

F#'s own idiomatic, value-based test style for
[AnyUnit](https://github.com/jbtule/AnyUnit) - a test is a `Test` value
built with the `test { }` computation expression, not a `[Test]`-
attributed instance method the way `AnyUnit.Style.Nunit`/
`AnyUnit.Style.Xunit`/`AnyUnit.Style.FsUnit` all work. If your F# test
suite already reaches for real NUnit/xUnit's attribute style, use one of
those instead - reach for this one when a top-level `let` reads more
naturally than a class full of `[Test]` methods.

## Usage

```fsharp
[<assembly: AnyUnit.Style.FSharp.FSharpStyle>]
do ()

open AnyUnit.Style.FSharp.Test

// Either shape is discovered - a plain value binding, or a real
// zero-argument function (the two idiomatic ways to write "just a test,
// no parameters" in F#):
let ``adds two numbers`` = test {
    let! Assert = assertion
    Assert.Equal(4, 2 + 2)
}

let ``adds two numbers, function form`` () = test {
    let! Assert = assertion
    Assert.Equal(4, 2 + 2)
}
```

`let! Assert = assertion` binds the current test's `IAssert` - call
straight into whichever style's assertion vocabulary you already have
referenced (`AnyUnit.Style.Xunit`'s `Assert.Equal`/`Assert.True`/...,
`AnyUnit.Constraints`' `Assert.That(x, Is.EqualTo(y))`, ...): this
package doesn't provide its own. `let! log = AnyUnit.Style.FSharp.Test.log`
gets the test's `ILog` (captured output) the same way.

A test is discovered by its own `let` binding name - there's no separate
description string to also keep in sync.

## Running directly, without discovery

`AnyUnit.Style.FSharp.Runner.run` executes a plain `(string * Test) list`
by hand and folds the results into a real `ResultsFile` - useful for a
host with no reflection-based discovery step at all:

```fsharp
open AnyUnit.Style.FSharp.Runner
run "net10" "MyTests" [ "adds two numbers", ``adds two numbers`` ]
```
