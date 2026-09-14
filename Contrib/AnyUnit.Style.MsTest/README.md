# AnyUnit.Style.MsTest

Roughly MSTest-compatible attributes and assertions for
[AnyUnit](https://github.com/jbtule/AnyUnit) - close enough to real
MSTest's own syntax that a test's logic often doesn't need to change, run
anywhere AnyUnit's core reaches (including browser-wasm) - not a full
reimplementation of MSTest's API (see "Not covered" below).

Unlike `AnyUnit.Style.Nunit` and `AnyUnit.Style.Xunit`, nothing in this
package is ported from the framework it imitates; every file is written
against MSTest's documented public shape, so the whole directory carries
AnyUnit's own Apache-2.0 header and there is no `License.txt` here.

## Moving an existing MSTest suite onto this

Usually a `PackageReference` swap plus two edits per file:

1. `using Microsoft.VisualStudio.TestTools.UnitTesting;` becomes
   `using AnyUnit.Style.MsTest;`
2. Each `[TestClass]` must derive from `AssertionHelper`:

```csharp
using AnyUnit.Style.MsTest;

[TestClass]
public class CalculatorTests : AssertionHelper   // <- the one required edit
{
    [TestInitialize]
    public void Setup() { ... }

    [TestMethod]
    public void Adds()
    {
        Assert.AreEqual(4, 2 + 2);
        StringAssert.Contains("hello world", "lo wo");
        CollectionAssert.AreEqual(new[] { 1, 2 }, Calculator.Pair());
    }
}
```

That base class is the porting tax, and it is the same one
`AnyUnit.Style.Nunit`'s `AssertionHelper` and `AnyUnit.Style.Xunit`'s
`TestClass` already charge: a real `[TestClass]` derives from nothing,
because MSTest's `Assert`, `StringAssert` and `CollectionAssert` are
static classes. AnyUnit's `Assert` is deliberately an *instance* scoped to
one test's run, so that a test which asserted nothing can be told apart
from one that asserted and passed (`NoError` vs `Success` - see
[AnyUnit's own README](../../AnyUnit) for why that distinction is worth
having). Deriving `AssertionHelper` is what makes the bare identifiers
`Assert`, `StringAssert`, `CollectionAssert` and `TestContext` inside a
test method resolve, unchanged, to that per-test instance.

Once the class derives it, ordinary call sites carry over verbatim -
`Assert.AreEqual(...)`, `CollectionAssert.AreEquivalent(...)`,
`TestContext.WriteLine(...)` all mean the same thing and read the same
way.

## Covers

- `[TestClass]`, `[TestMethod]`, `[DataTestMethod]`
- `[TestInitialize]` / `[TestCleanup]`, `[ClassInitialize]` /
  `[ClassCleanup]`
- `[AssemblyInitialize]` / `[AssemblyCleanup]` - see the caveat below
- `[DataRow]` (including `[DataRow(null)]`), `[DynamicData]` (property or
  method source, own type or a foreign one)
- `[Ignore]` (method and class), `[Timeout]`, `[Description]`,
  `[TestCategory]`, `[Owner]`, `[Priority]`, `[TestProperty]`
- `[ExpectedException]`, including `AllowDerivedTypes` and MSTest's
  exact-type-match default
- `Assert.AreEqual` / `AreNotEqual` (including the `double` + delta and
  `string` + `ignoreCase` overloads), `AreSame` / `AreNotSame`, `IsTrue` /
  `IsFalse`, `IsNull` / `IsNotNull`, `IsInstanceOfType` /
  `IsNotInstanceOfType`, `ThrowsException<T>`, `Fail`, `Inconclusive`
- `StringAssert.Contains` / `DoesNotContain` / `StartsWith` / `EndsWith` /
  `Matches` / `DoesNotMatch`
- `CollectionAssert.AreEqual` / `AreNotEqual` / `AreEquivalent` /
  `AreNotEquivalent` / `Contains` / `DoesNotContain` /
  `AllItemsAreNotNull` / `AllItemsAreUnique` / `IsSubsetOf`
- A minimal `TestContext`: `TestName`, `FullyQualifiedTestClassName`,
  `CurrentTestOutcome`, `WriteLine`

`Assert.Inconclusive` maps onto `AnyUnit.IgnoreException`, so an
inconclusive test is reported as skipped by every format
[`AnyUnit.Report`](../../Report) writes - an exact mapping, not an
approximation.

## Cross-style data rows

`[DataRow]` implements `AnyUnit.Run.Attributes.IRowInlineParameter` and
`[DynamicData]` implements `IGeneratingParameter`, which are core
interfaces every style package already references. So a
`[DataTestMethod]` accepts xUnit `[InlineData]` and NUnit `[TestCase]`
rows, and `[DataRow]` feeds an xUnit `[Theory]` or an NUnit `[Test]` -
with no reference between the style packages in either direction.
`WhoTestsTheTesters/Tests/Style/ComboTests` keeps that honest.

## Not covered

- **`[AssemblyInitialize]` needs one extra attribute.** AnyUnit discovers
  its once-per-scope hook (`SetUpFixtureAttributeBase`) as a *class-level*
  attribute, whereas MSTest infers the class from the marked methods, so
  the holding class must also be marked `[AssemblySetUp]`:

  ```csharp
  [TestClass]
  [AssemblySetUp]                      // <- not an MSTest attribute
  public class Global
  {
      [AssemblyInitialize]
      public static void Init(TestContext context) { ... }
  }
  ```

  Also note AnyUnit scopes that to the class's own namespace and its
  sub-namespaces, not to the literal assembly. Put the class in the
  suite's root namespace - where a ported one almost always already sits -
  and the two coincide.

- **`TestContext` stops at four members.** `TestName`,
  `FullyQualifiedTestClassName`, `CurrentTestOutcome` and `WriteLine` are
  there because ported suites read them and because `[ClassInitialize]`'s
  signature does not compile without the type existing at all. The
  deployment API (`TestDeploymentDir`, `TestRunDirectory`,
  `DeploymentDirectory`, `AddResultFile`) is absent because AnyUnit has no
  deployment step - use `AppContext.BaseDirectory` for a path. The old
  database-backed data-driven API (`TestContext.DataRow`,
  `DataConnection`, `Properties`) is absent because `[DataRow]` /
  `[DynamicData]` cover what people still write. `BeginTimer`/`EndTimer`
  are absent.

- **`CurrentTestOutcome` is coarse.** It reports `InProgress`, then
  `Passed`, `Failed`, `Inconclusive` or `Error`. `Timeout`, `Aborted`,
  `NotRunnable` and `Unknown` exist in the enum for source compatibility
  but are never produced - a timeout is enforced by the core *outside* the
  test's own invocation (see `AnyUnit.Run.Test.Run`), so there is no
  moment at which a `[TestCleanup]` could observe one.

- **`[TestProperty]`, `[Owner]` and `[Priority]` are flattened into
  categories** (`"key=value"`, `"Owner:name"`, `"Priority:1"`) because the
  results schema carries only a flat category list today. Real key/value
  properties are a separate in-flight change; when that lands these move
  and only true `[TestCategory]` names stay in the category list.

- **`[DataRow(..., DisplayName = "...")]` compiles but is ignored.**
  AnyUnit names a row from its arguments and has no slot for an explicit
  display name.

- **No parameterized test classes** - MSTest has no such concept, so
  `[TestClass]` takes no constructor arguments.

- **Other MSTest attributes** - `[DeploymentItem]`, `[TestInitialize]`
  ordering attributes, `[DoNotParallelize]` / `[Parallelize]` (AnyUnit's
  runner is strictly sequential, so parallelism attributes would be
  meaningless), `[STATestMethod]`, `[CssIteration]` /
  `[CssProjectStructure]`, and `[WorkItem]` are not implemented.

- **No static `Assert`.** Same deliberate design as every other AnyUnit
  style: a shared/global assert cannot tell a test that made real
  assertions apart from one that made none. A plain helper class that is
  not itself a fixture and wants to assert throws
  `AnyUnit.AssertionException` / `AnyUnit.IgnoreException` directly.

- **`async Task` test methods.** AnyUnit's core does not await a returned
  `Task` yet, so an `async` test's failures can go unnoticed. That is a
  core limitation, not a MSTest-style one, and is being fixed separately.

## Validated against a real suite

Ported [ParksComputing.Xfer.Lang.Tests](https://github.com/paulmooreparks/Xfer)
- **595 test methods, 50 test classes, 60 files** - which passes 595/595
under real MSTest 3.9.3.

After the port it runs on AnyUnit with **the same 595 tests and no
failures**.

The whole port, end to end:

| Edit | Count |
|---|---|
| `<Using Include="...UnitTesting" />` -> `AnyUnit.Style.MsTest` in the csproj | 1 |
| Swap the two MSTest `PackageReference`s for a reference to this package | 1 |
| `using Microsoft.VisualStudio.TestTools.UnitTesting;` -> `using AnyUnit.Style.MsTest;` | 50 files |
| Add `: AssertionHelper` to each `[TestClass]` | 50 classes |
| Cast a `dynamic` argument (see below) | 3 call sites |

Everything else - `[TestMethod]`, `[TestClass]`, `[TestInitialize]`,
`[TestCleanup]`, 23 `[ExpectedException]`, and ~1,570 assertion calls
across `Assert`, `StringAssert` and `CollectionAssert` - compiled and
ran unchanged.

### Two gaps it found, both now fixed

`Assert.IsInstanceOfType<T>(value)` (MSTest 3.x's generic overload, as
opposed to the `(value, Type)` one) and `Assert.ThrowsExceptionAsync<T>`
were both missing. Added.

### One limit that cannot be fixed

`Assert` here is a set of extension methods on `IAssert`, and **C# cannot
dispatch an extension method on a `dynamic` argument**:

```csharp
dynamic? result = Deserialize<dynamic>(text);
Assert.IsNotNull(result);           // CS1973 here, fine in real MSTest
Assert.IsNotNull((object?)result);  // the fix: cast
```

Real MSTest's `Assert` is a static class, so it has no such problem. This
is inherent to how a style adds vocabulary to `IAssert` and is the one
edit a port may need beyond the base class. It affected 3 call sites out
of ~1,570.

### What it also surfaced about the suite

Five of the 595 tests report `NoError` rather than `Success` - they
assert nothing at all. One is a debug leftover that only writes to the
console inside a try/catch; another is a "does not throw" test with no
assertion. Both are green under MSTest, which cannot tell them apart
from a real pass. Distinguishing exactly that is why AnyUnit's `Assert`
is an instance scoped to one test.
