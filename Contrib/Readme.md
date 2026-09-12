## Contrib

Style packages - each one layers a specific, real test framework's
source-level API on top of [`AnyUnit`](../AnyUnit)'s core attributes. See
each package's own README for what it covers and how to use it:

- [**AnyUnit.Constraints**](AnyUnit.Constraints) - NUnit-style
  `Is`/`Has`/`Does`/`Throws` fluent constraint syntax.
- [**AnyUnit.Style.Nunit**](AnyUnit.Style.Nunit) - NUnit-source-compatible
  attributes and assertions.
- [**AnyUnit.Style.Xunit**](AnyUnit.Style.Xunit) - xUnit-source-compatible
  attributes and assertions.
- [**AnyUnit.Style.FSharp**](AnyUnit.Style.FSharp) - F#'s own idiomatic
  value-based test style.
- [**AnyUnit.Style.FsUnit**](AnyUnit.Style.FsUnit) - FsUnit-style F#
  assertions.

Real NUnit/xUnit source this repo ports from is attributed in
[`Contributors.md`](../Contributors.md).
