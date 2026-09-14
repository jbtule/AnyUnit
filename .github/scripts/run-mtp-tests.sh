#!/bin/bash
# Runs all 8 *.Mtp self-test projects through Microsoft.Testing.Platform,
# writing each one's AnyUnit results file to /tmp/<name>-<suffix>.json.
#
# The console-runner twin of this is run-tests.sh, which drives the same
# self-test payload assemblies through a built anyunit-runner instead.
# They can't be one script: an .Mtp project is a self-hosting executable
# (it compiles the payload's sources in and generates its own MTP entry
# point - see AnyUnit.TestingPlatform/README.md), so there is no external
# runner to point at a .dll here, and the output flag is MTP's own
# --report-anyunit-json rather than anyunit-runner's -o.
#
# Usage: run-mtp-tests.sh <suffix>
#   suffix   tag appended to each output filename, e.g. net10-mtp. Note
#            this only names the FILE; the Platform value inside the JSON
#            already ends in "-mtp" on its own (PlatformId.Current +
#            "-mtp", see AnyUnitTestFramework), so an MTP run is
#            distinguishable from the same assembly's console-runner run
#            without any -p/-platform-suffix equivalent here.
#
# Same "the exit code is not the signal" contract as run-tests.sh: every
# one of these assemblies deliberately contains Fail/Error cases, so a
# non-zero exit is the expected, correct outcome. The real gate is
# WhoTestsTheTesters/ConventionTestProcessor over the JSON these produce,
# which is the entire reason this script exists - before
# --report-anyunit-json, the MTP leg produced nothing ConventionTestProcessor
# could read, and was checked only for "a .trx came out and parses".
set -e

suffix="$1"
if [ -z "$suffix" ]; then
  echo "run-mtp-tests.sh: usage: run-mtp-tests.sh <suffix>" >&2
  exit 1
fi

run() {
  local name="$1"
  local dir="$2"
  local exe="WhoTestsTheTesters/Tests/$dir/bin/Release/net10.0/$name.dll"
  # Same fail-loudly ladder as run-tests.sh, for the same confirmed-real
  # reasons - see that script's comments. A missing executable means the
  # project never got built (e.g. dropped from AnyUnit.ci.slnf); without
  # this check the run would simply be absent from the gate, which only
  # ever looks at whichever /tmp/*.json files happen to exist.
  if [ ! -f "$exe" ]; then
    echo "run-mtp-tests.sh: expected MTP test executable not found: $exe" >&2
    exit 1
  fi
  local out="/tmp/$name-$suffix.json"
  rm -f "$out"
  # The path is its own argv entry, never interpolated into a larger
  # string: git-bash's MSYS layer translates a whole POSIX-path argument
  # for a native process but not an embedded one, and that mismatch is a
  # confirmed real Windows CI failure (see build.yml's report-trx step).
  # Passing it whole here is exactly what run-tests.sh already does with
  # anyunit-runner's own -o.
  dotnet "$exe" --report-anyunit-json "$out" || true
  if [ ! -f "$out" ]; then
    echo "run-mtp-tests.sh: '$name' produced no output file at $out" >&2
    exit 1
  fi
  if ! python3 -c '
import json, sys
with open(sys.argv[1], encoding="utf-8-sig") as fh:
    doc = json.load(fh)
count = sum(len(t.get("Results", []))
            for a in doc.get("Assemblies", [])
            for f in a.get("Fixtures", [])
            for t in f.get("Tests", []))
if count == 0:
    sys.exit(1)
' "$out"; then
    echo "run-mtp-tests.sh: '$name' produced $out with zero results - MTP ran but discovered no tests" >&2
    exit 1
  fi
}

run BasicTests.Mtp BasicTests.Mtp
run ConstraintsTests.Mtp ConstraintsTests.Mtp
run NunitTests.Mtp Style/NunitTests.Mtp
run XunitTests.Mtp Style/XunitTests.Mtp
run FsUnitTests.Mtp Style/FsUnitTests.Mtp
run FSharpTests.Mtp Style/FSharpTests.Mtp
run ComboTests.Mtp Style/ComboTests.Mtp
run ComboTests.FSharp.Mtp Style/ComboTests.FSharp.Mtp
