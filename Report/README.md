# AnyUnit.Report

Standalone .NET tool (`anyunit-report`) that converts an
[AnyUnit](https://github.com/jbtule/AnyUnit) JSON results file (the
`-o`/`-output <file>` output of `anyunit-runner run`, or any other
AnyUnit runner) into a report format your CI system, IDE, or dashboard
already understands: JUnit XML, TRX, NUnit3 XML, xUnit2 XML, or
[CTRF](https://ctrf.io) JSON.

## Install

```
dotnet tool install --global AnyUnit.Report
```

(Not yet published to nuget.org - grab a prerelease build from the
repo's own `pack` workflow artifacts, or build from source:
`dotnet pack Report/AnyUnit.Report.csproj`.)

## Usage

```
anyunit-report convert -f|-format <junit|trx|nunit|xunit|ctrf> -o|-output <file> <results.json> [<results2.json> ...]
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
