# AnyUnit.Constraints

NUnit-style fluent constraint syntax for
[AnyUnit](https://github.com/jbtule/AnyUnit) - `Is`, `Has`, `Does`,
`Throws`, and the `Assert.That(actual, constraint)` form they compose
with. Ported from real NUnit's own constraint model (see
[`Contributors.md`](../../Contributors.md) for attribution), so existing
`Assert.That(x, Is.EqualTo(y))`-style assertions read the same here.

Referenced transitively by [`AnyUnit.Style.Nunit`](../AnyUnit.Style.Nunit)
and [`AnyUnit.Style.FsUnit`](../AnyUnit.Style.FsUnit) - install one of
those instead of this package directly unless you specifically want the
constraint syntax without a full NUnit-style `[Test]`/`[TestFixture]`
attribute set.

## Example

```csharp
Assert.That(actual, Is.EqualTo(expected));
Assert.That(collection, Has.Count.EqualTo(3));
Assert.That(text, Does.StartWith("prefix"));
Assert.That(() => DoSomething(), Throws.InstanceOf<InvalidOperationException>());
```
