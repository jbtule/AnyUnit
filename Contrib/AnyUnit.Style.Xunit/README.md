# AnyUnit.Style.Xunit

Roughly xUnit-compatible attributes and assertions for
[AnyUnit](https://github.com/jbtule/AnyUnit) - close enough to real
xUnit.net's own syntax (v1-shaped API) that a test's logic often doesn't
need to change, run anywhere AnyUnit's core reaches (including
browser-wasm) - not a full reimplementation of xUnit's API. Ported from
real xUnit's own source (see [`Contributors.md`](../../Contributors.md)
for attribution).

An assembly opts in with `[assembly: XunitStyle]`
([`XunitStyleAttribute`](XunitStyleAttribute.cs)) so this style's
`[Fact]`/`[Theory]` classes are recognized as fixtures without a
`[TestFixture]`-shaped attribute on every class (matching real xUnit's
own class-implicit-fixture convention).

## Covers

- `[Fact]`, `[Theory]` with `[InlineData]`/`[ClassData]`/`[PropertyData]`
- `Assert.*` (the classic xUnit v1 assertion surface -
  `Equal`/`NotEqual`/`True`/`False`/`Null`/`NotNull`/`Throws`/`Contains`/
  `DoesNotContain`/`InRange`/... - see [`AssertEx.cs`](AssertEx.cs))
- `[Trait]`
- `IUseFixture<T>`
