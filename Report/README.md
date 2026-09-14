# AnyUnit.Report

Standalone .NET tool (`anyunit-report`) that converts an
[AnyUnit](https://github.com/jbtule/AnyUnit) JSON results file (the
`-o`/`-output <file>` output of `anyunit-runner run`, or any other
AnyUnit runner) into a report format your CI system, IDE, or dashboard
already understands: JUnit XML, TRX, NUnit3 XML, xUnit2 XML, or
[CTRF](https://ctrf.io) JSON - or into a self-contained HTML report
built around AnyUnit's multi-platform results (see
[HTML](#html-report)), or a GitHub Actions job summary (see
[Markdown](#markdown-summary)).

## Install

```
dotnet tool install --global AnyUnit.Report
```

(Prerelease builds of unreleased work are published as artifacts of the
repo's own `pack` workflow run, or build from source:
`dotnet pack Report/AnyUnit.Report.csproj`.)

## Usage

```
anyunit-report convert -f|-format <junit|trx|nunit|xunit|ctrf|html|markdown> -o|-output <file> <results.json> [<results2.json> ...]
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

## Markdown summary

`-f markdown` emits GitHub Flavored Markdown sized for a [GitHub Actions
job summary](https://docs.github.com/en/actions/reference/workflow-commands-for-github-actions#adding-a-job-summary)
- the run's headline numbers, a per-platform table, and the failing
tests with an output excerpt each.

It's the companion to `-f html`, not a replacement: HTML is exhaustive
and lives in an artifact you download, Markdown is the bounded "what
happened, do I need to look?" view that renders straight into the
Actions UI with nothing to click through.

### In a workflow

```yaml
- name: Run tests
  run: dotnet anyunit-runner.dll run -o results.json MyTests.dll

- name: Post test summary
  if: always()
  run: |
    anyunit-report convert -f markdown -o summary.md results.json
    cat summary.md >> "$GITHUB_STEP_SUMMARY"
```

`if: always()` matters: a failing test run is exactly when you want the
summary, and without it the step is skipped on failure. Pass several
files (`results-*.json`) to summarise a whole platform matrix in one
post - the per-platform table is the point of doing so.

To publish the full report alongside it, add the HTML as an artifact:

```yaml
- name: Upload full report
  if: always()
  uses: actions/upload-artifact@v4
  with:
    name: test-report
    path: report.html
```

### Staying under the limit

A job summary is capped at **1 MiB per step**, and exceeding it fails
the *upload* with an error annotation while the job still reports
success - so an oversized summary silently doesn't appear at all. This
writer is therefore bounded by construction rather than by hoping:

- Failures are grouped **per test**, not per (test, platform) - a test
  failing on 18 platforms is one entry, not 18.
- A test that fails on *every* platform says so; the platform list is
  enumerated only when it's a subset, which is the case where which
  platforms is the actual diagnosis.
- At most 50 failing tests are listed, with a 20-line / 2000-character
  output excerpt each, taken from the first failing platform.
- A whole-document byte budget backstops the rest, rewinding to the last
  balanced point so truncation can't leave a dangling `<details>`.

Whenever anything is left out, the summary says so and points at the
HTML report. For scale: this repo's own 18-platform CI run (374 tests,
6718 results, 2830 of them failing by design) produces a ~23 KB summary,
about 2% of the limit.
