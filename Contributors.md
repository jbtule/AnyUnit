# Contributors

Original Author: Jay Tuley <jay+code@tuley.name> @jbtule

## Upstream projects this repo ports from

Several of the style packages didn't start from a blank file - they
started from the real framework's own source, which was then altered to
build on AnyUnit's core. Those files keep their original project's
copyright header, so **this repo has genuinely mixed per-file
provenance**: two files in the same directory can carry different
copyright holders and different licenses, and that's correct rather than
an inconsistency to tidy up. Each affected directory also carries its
own `License.txt` with the upstream terms in full.

The repo as a whole is [Apache-2.0](License.txt), and every package's
`<PackageLicenseExpression>` says so; the notices below are the
attribution those upstream licenses require, not a different license for
the package.

| Directory | Ported from | Upstream copyright | Upstream license |
|---|---|---|---|
| [`Contrib/AnyUnit.Constraints`](Contrib/AnyUnit.Constraints) | [NUnit](https://nunit.org) - the `Is`/`Has`/`Does`/`Throws` constraint model | Charlie Poole (2007-2012); James W. Newkirk, Michael C. Two, Alexei A. Vorontsov (2002-2004); Philip A. Craig (2000-2002) | [NUnit license](Contrib/AnyUnit.Constraints/License.txt) (zlib-style) |
| [`Contrib/AnyUnit.Style.Nunit`](Contrib/AnyUnit.Style.Nunit) | [NUnit](https://nunit.org) - various components | Charlie Poole (2007-2009), and the earlier NUnit holders above | [NUnit license](Contrib/AnyUnit.Style.Nunit/License.txt) (zlib-style) |
| [`Contrib/AnyUnit.Style.Xunit`](Contrib/AnyUnit.Style.Xunit) | [xUnit.net](https://xunit.net) - assertions and exception types | Outercurve Foundation (2013) | [Apache-2.0](Contrib/AnyUnit.Style.Xunit/License.txt) |
| [`Contrib/AnyUnit.Style.FsUnit`](Contrib/AnyUnit.Style.FsUnit) | [FsUnit](https://fsprojects.github.io/FsUnit/) - the `should` operator vocabulary | Raymond Vernagus (2010) | [MIT](Contrib/AnyUnit.Style.FsUnit/License.txt) |

Written for this repo, not ported from anywhere:
[`AnyUnit`](AnyUnit) (the core), [`AnyUnit.TestingPlatform`](AnyUnit.TestingPlatform),
[`Contrib/AnyUnit.Style.FSharp`](Contrib/AnyUnit.Style.FSharp),
[`Report`](Report), [`Runner`](Runner), and
[`WhoTestsTheTesters`](WhoTestsTheTesters) - plus the parts of the ported
directories above that are AnyUnit's own (roughly half of
`AnyUnit.Style.Nunit`, for instance). Those files carry the
`Copyright 2013 AnyUnit Contributors` Apache-2.0 header.

### Which header a file should have

The rule is per file, by where that file's code actually came from -
never per directory:

- Written for this repo → the `Copyright 2013 AnyUnit Contributors`
  Apache-2.0 block.
- Ported from NUnit → NUnit's own banner header, pointing at
  <https://nunit.org>.
- Ported from xUnit.net → the `Copyright 2013 Outercurve Foundation`
  Apache-2.0 block.
- Ported from FsUnit → a notice naming Raymond Vernagus and pointing at
  the directory's `License.txt`.

Both the NUnit and FsUnit licenses require that altered versions be
plainly marked as such and that the origin not be misrepresented, which
is what those headers and each directory's `License.txt` are doing.
