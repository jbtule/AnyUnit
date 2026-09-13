# AnyUnit.Report

Standalone .NET tool (`anyunit-report`) that converts an
[AnyUnit](https://github.com/jbtule/AnyUnit) JSON results file (the
`-o`/`-output <file>` output of `anyunit-runner run`, or any other
AnyUnit runner) into a report format your CI system, IDE, or dashboard
already understands: JUnit XML, TRX, NUnit3 XML, xUnit2 XML, or
[CTRF](https://ctrf.io) JSON - or into a self-contained HTML report
built around AnyUnit's multi-platform results (see
[HTML](#html-report)).

## Install

```
dotnet tool install --global AnyUnit.Report
```

(Not yet published to nuget.org - grab a prerelease build from the
repo's own `pack` workflow artifacts, or build from source:
`dotnet pack Report/AnyUnit.Report.csproj`.)

## Usage

```
anyunit-report convert -f|-format <junit|trx|nunit|xunit|ctrf|html> -o|-output <file> <results.json> [<results2.json> ...]
```

One format per run - to produce more than one, run it more than once
against the same `results.json`. Example:

```
anyunit-runner run -o results.json MyTests.dll
anyunit-report convert -f junit -o results.junit.xml results.json
anyunit-report convert -f trx -o results.trx results.json
```

A single `results.json` already represents multiple platforms fine on
its own (the same test run under more than one platform becomes one
`<testcase>`/`<test>` per platform, disambiguated as `"TestName
[platform]"`) - that's the normal case, no extra step needed. Passing
more than one `results.json` (e.g. one per runner, the way
`.github/scripts/run-tests.sh` in this repo produces them) merges them
into that same shape first, so a separate net10/net48/browser-wasm run
of the same assemblies converts into one report:

```
anyunit-report convert -f junit -o results.junit.xml results-net10.json results-net48.json
```

## HTML report

`-f html` is the one format here that isn't an existing external schema,
so it's the only one free to be shaped around what actually makes
AnyUnit's results different: the same test run under several platforms
at once.

The other five formats all assume one result per test, so a
multi-platform test has to be flattened into N separate `"TestName
[platform]"` test cases - the honest mapping into those schemas, but it
scatters one test's story across N unrelated-looking rows. The HTML
report keeps platform as an axis instead:

- A **test × platform matrix** - one row per test, one column per
  platform - so "passes everywhere except net48-win-x86" is a glance
  rather than a diff of two reports.
- **Gaps are visible.** A test × platform combination with no result at
  all (a platform that never ran that assembly) renders as `·` rather
  than being silently omitted, which is the other thing the flattened
  formats can't express.
- **Per-platform environment cards** - runtime, OS and architecture as
  actually captured per result, not one global guess.
- Filtering by name, failures-only, and per-platform column toggles;
  fixtures with failures start expanded, green ones start collapsed.

It's a **single self-contained file** - inline CSS/JS, no external
references, no network - so it works straight out of a CI artifact zip
over `file://`. It's responsive down to phone widths, follows the
reader's light/dark preference, and marks outcomes with a glyph as well
as a colour so the matrix stays readable with a colour vision
deficiency. The page renders completely with JavaScript disabled (the
script only adds filtering and collapsing).

```
anyunit-report convert -f html -o report.html results-*.json
```
