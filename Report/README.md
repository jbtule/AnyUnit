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
anyunit-report convert -f|-format <junit|trx|nunit|xunit|ctrf> -o|-output <file> <results.json>
```

One format per run - to produce more than one, run it more than once
against the same `results.json`. Example:

```
anyunit-runner run -o results.json MyTests.dll
anyunit-report convert -f junit -o results.junit.xml results.json
anyunit-report convert -f trx -o results.trx results.json
```
