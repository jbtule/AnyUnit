# AnyUnit.Style.Nunit

Roughly NUnit-compatible attributes and assertions for
[AnyUnit](https://github.com/jbtule/AnyUnit) - close enough to real
NUnit's own syntax that a test's logic often doesn't need to change, run
anywhere AnyUnit's core reaches (including browser-wasm) - not a full
reimplementation of NUnit's API (see "Not covered" below). Ported from
real NUnit's own source (see [`Contributors.md`](../../Contributors.md)
for attribution).

Moving an existing NUnit test project onto this is usually a
`PackageReference` swap, not a source rewrite - `using NUnit.Framework;`
becomes `using AnyUnit.Run; using AnyUnit.Style.Nunit; using AnyUnit.Constraints;`,
and a fixture class needs to derive `AssertionHelper` for its `Assert`/
`Log` to resolve. Once it does, `Assert.That(...)` and every other call
inside an ordinary test method carries over completely unchanged - it's
just resolving to that instance now, the same identifier either way.
The one real, load-bearing difference (AnyUnit's `Assert` is an instance
property, not the fully-static class real NUnit's is - see [AnyUnit's own
README](../../AnyUnit) for why) only actually shows up in a *plain
helper class that isn't itself a fixture* and wants to assert - a real
but narrow case, not something ordinary test methods ever run into.

## Covers

- `[TestFixture]` (including parameterized: `[TestFixture(args)]`),
  `[SetUpFixture]`
- `[Test]`, `[TestCase]` (including per-case `Ignore = "reason"`),
  `[Theory]` (including automatic enum-value expansion for a
  parameter with no explicit data source)
- `[SetUp]` / `[TearDown]` / `[OneTimeSetUp]` / `[OneTimeTearDown]`
- `[Values]` / `[ValueSource]` / `[Range]` / `[Random]`
- `[Ignore]`, `[Category]`, `[Property]`, `[Author]`, `[Description]`, `[Platform]`, `[Timeout]`
- `ITestAction` (method/fixture-level before/after hooks)
- `Assert.That` and the `Is`/`Has`/`Does`/`Throws` constraints (via
  [`AnyUnit.Constraints`](../AnyUnit.Constraints)), and the classic
  model over the same constraints: `AreEqual`, `AreNotEqual`, `AreSame`,
  `AreNotSame`, `IsTrue`, `IsFalse`, `IsNull`, `IsNotNull`, `NotNull`,
  `Greater`, `GreaterOrEqual`, `Less`, `LessOrEqual`, `Zero`, `NotZero`,
  `IsInstanceOf<T>`, `IsNotInstanceOf<T>`, `IsEmpty`, `IsNotEmpty`,
  `Contains`, `Throws<T>`, `Catch<T>`, `DoesNotThrow`, `Pass`.
  `AreEqual` is NUnit's equality, so `AreEqual(1, 1L)` passes; `Throws<T>`
  returns the caught exception to assert on

## Not covered

Real NUnit's `TestContext` (no equivalent - use `AppContext.BaseDirectory`
for a working-directory-relative path instead) and its fully-static
`Assert`/`Assume`. The static-`Assert` gap is deliberate, not an
oversight: a shared/global assert can't reliably tell a test that made
real assertions and passed apart from one that made none at all and
trivially "passed" by doing nothing, whereas an instance scoped to
exactly one test's own run tracks that correctly (`AssertCount`, behind
the `Success`/`NoError` result distinction). A non-fixture helper class
that wants to assert throws `AnyUnit.AssertionException`/`IgnoreException`
directly instead of calling a static `Assert.Fail`/`Assert.Ignore` - the
one place this actually comes up in practice (an ordinary test method
already has a real `Assert` to call, via its fixture).
