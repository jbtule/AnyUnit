## Contrib

Style packages - each one gets close enough to a specific, real test
framework's own syntax that an existing test's logic often doesn't need
to change, on top of [`AnyUnit`](../AnyUnit)'s core attributes - not a
full reimplementation of that framework's API. See each package's own
README for what it actually covers and how to use it:

- [**AnyUnit.Constraints**](AnyUnit.Constraints) - NUnit-style
  `Is`/`Has`/`Does`/`Throws` fluent constraint syntax.
- [**AnyUnit.Style.Nunit**](AnyUnit.Style.Nunit) - roughly NUnit-compatible
  attributes and assertions.
- [**AnyUnit.Style.Xunit**](AnyUnit.Style.Xunit) - roughly xUnit-compatible
  attributes and assertions.
- [**AnyUnit.Style.MsTest**](AnyUnit.Style.MsTest) - roughly
  MSTest-compatible attributes and assertions.
- [**AnyUnit.Style.FSharp**](AnyUnit.Style.FSharp) - F#'s own idiomatic
  value-based test style.
- [**AnyUnit.Style.FsUnit**](AnyUnit.Style.FsUnit) - FsUnit-style F#
  assertions.

Real NUnit/xUnit source this repo ports from is attributed in
[`Contributors.md`](../Contributors.md).

Frameworks that were considered and deliberately not given a style are
written up rather than left implicit - see
[`TUnit-style-evaluation.md`](TUnit-style-evaluation.md).
