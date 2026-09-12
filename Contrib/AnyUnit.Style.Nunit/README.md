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
`Log` to resolve (AnyUnit's `Assert` is an instance property a fixture
gets, not the fully-static class real NUnit's is - a real, load-bearing
difference for a plain helper class that isn't itself a fixture; see
[AnyUnit's own README](../../AnyUnit) for the assertion model this all
sits on).

## Covers

- `[TestFixture]` (including parameterized: `[TestFixture(args)]`),
  `[SetUpFixture]`
- `[Test]`, `[TestCase]` (including per-case `Ignore = "reason"`),
  `[Theory]` (including automatic enum-value expansion for a
  parameter with no explicit data source)
- `[SetUp]` / `[TearDown]` / `[OneTimeSetUp]` / `[OneTimeTearDown]`
- `[Values]` / `[ValueSource]` / `[Range]` / `[Random]`
- `[Ignore]`, `[Category]`, `[Description]`, `[Platform]`, `[Timeout]`
- `ITestAction` (method/fixture-level before/after hooks)
- `Assert.That`/`Assert.AreEqual`/classic-model asserts, `Is`/`Has`/`Does`/
  `Throws` constraints (via [`AnyUnit.Constraints`](../AnyUnit.Constraints))

## Not covered

Real NUnit's `TestContext` (no equivalent - use `AppContext.BaseDirectory`
for a working-directory-relative path instead) and its fully-static
`Assert`/`Assume` (AnyUnit's `Assert` is an instance member of a fixture's
own `AssertionHelper` base - a plain helper class that isn't a fixture
throws `AnyUnit.AssertionException`/`IgnoreException` directly instead of
calling a static `Assert.Fail`/`Assert.Ignore`).
