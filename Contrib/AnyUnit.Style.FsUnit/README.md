# AnyUnit.Style.FsUnit

[FsUnit](https://github.com/fsprojects/FsUnit)-style F# assertions for
[AnyUnit](https://github.com/jbtule/AnyUnit) - `x |> should equal y`-shaped
assertions, on top of [`AnyUnit.Constraints`](../AnyUnit.Constraints)'
constraint model. Assertions only - pair this with
[`AnyUnit.Style.Nunit`](../AnyUnit.Style.Nunit) or a similar attribute
style for `[Test]`/`[TestFixture]` discovery.

## Usage

A test fixture derives `AssertionHelper` (this package's own, not
`AnyUnit.Style.Nunit`'s - both ultimately derive the same core
`AnyUnit.AssertionHelper`, so its `should`/`shouldFail` still resolve
correctly against whichever style's `Assert` the fixture ends up with)
for the instance-scoped `this.should`/`this.shouldFail`:

```fsharp
type MyTests() =
    inherit AnyUnit.Style.FsUnit.AssertionHelper()

    [<Test>]
    member this.``adds two numbers``() =
        (2 + 2) |> this.should equal 4

    [<Test>]
    member this.``throws on bad input``() =
        this.shouldFail (fun () -> failwith "boom")
```

`this.printf`/`this.printfn` write to the test's captured `Log` the same
way `Console.printf` would write to the console.

Free-function `should`/`shouldFail` (no `this.` receiver) work exactly as
in real FsUnit, including from a module-level `[<Fact>] let` test with no
`this` at all: they assert through the running test (`AnyUnit.Run.
AmbientTest`, which the engine sets around every test body), so the
counts are real and a test with no assertion still reports `NoError`.
Before 1.2.1 they went through the global `Assert.GlobalStyle` and were
`[<Obsolete>]` for it; they now only fall back to that outside a running
test.
