# AnyUnit.Style.FSharp

Tests as *values* for [AnyUnit](https://github.com/jbtule/AnyUnit): a
test is a `Test` built with the `test { }` computation expression, not
an attributed method the way `AnyUnit.Style.Nunit`/`AnyUnit.Style.Xunit`/
`AnyUnit.Style.FsUnit` work.

## When to use this - and when not to

Plain F# module-level tests do **not** need this style. A top-level
`[<Fact>] let ``adds`` () = 1 + 1 |> should equal 2` is discovered by
`AnyUnit.Style.Xunit` as it is, and its bare `should` (or
`AnyUnit.Run.Assert.Current` for any style's assertion vocabulary)
asserts through the running test with real per-test counting. That is
the least ceremony for the common case, and what a suite arriving from
real xUnit + FsUnit already looks like. (This style predates that: it was
written when a module-level `let` had no way to reach a test's own
`IAssert` without a class to hang it on. `AnyUnit.Run.AmbientTest` closed
that gap in 1.2.1.)

Reach for `test { }` when the test being a value is the point:

- **Composition** - a `Test` can be built by a function, stored in a
  list, or wrapped by another `test { }` (`return! inner.Run`), which an
  attributed method can't be.
- **Start/stop resources** - `use`/`use!` inside the body gives a
  disposable a scope that is exactly the test, with no setup/teardown
  attribute pair to keep in step.
- **The log as a value** - `let! log = log` alongside `let! Assert =
  assertion`, both from the same running test.


## F#: `open` order matters

F# has no C# `CS0104`. When two opened namespaces export the same simple
name the **last `open` wins, silently** - no error, no warning. Put the
style's `open` where it will not be shadowed:

```fsharp
open AnyUnit                // exceptions, TestCapabilities, [<RequiresCapability>]
open AnyUnit.Constraints
open AnyUnit.Style.Nunit    // the attribute style - LAST
```

Opening two *attribute* styles at once (`AnyUnit.Style.Nunit` and
`AnyUnit.Style.Core`, say, or `.Nunit` and `.MsTest`, which all export an
`AssertionHelper`) is the case to avoid: whichever comes last provides
`[<Test>]`/`[<TestFixture>]`, and since each style's own
`TestAttribute.TestInvoke` is what dispatches that style's `[<SetUp>]`/
`[<TearDown>]`/`[<Ignore>]`, picking up the wrong one stops a fixture's
setup and teardown running with nothing to say so. Before 1.3.0 this
could happen by accident, because the core's built-in style shared the
`AnyUnit` namespace with the exception types; it now lives in
`AnyUnit.Style.Core` (#66). `ComboTests.FSharp/OpenOrder.fs` pins it.

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

## Parameterized tests

This style has no data attribute of its own by design (a `Test` is a
value, not a method with parameters an attribute could sit on) - a real
F# *function* (with real parameters, so it compiles to a genuine method,
not a property) carrying another style's row attribute works instead,
since `Test`-typed function discovery recognizes any attribute
implementing `AnyUnit.Run.Attributes.IRowInlineParameter`:

```fsharp
open AnyUnit.Style.FSharp.Test
open AnyUnit.Style.Xunit // for InlineDataAttribute - any style's own row attribute works the same way

[<InlineData(1, 1)>]
[<InlineData(2, 4)>]
[<InlineData(3, 9)>]
let square (n: int) (expected: int) = test {
    let! Assert = assertion
    Assert.True(n * n = expected, $"Expected {n}*{n} = {expected}")
}
```

Each row runs as its own, separately-reported test - same as `[TestCase]`/
`[InlineData]` under the attribute-based styles.
