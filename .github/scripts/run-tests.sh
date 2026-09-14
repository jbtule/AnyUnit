#!/bin/bash
# Runs all 8 self-test assemblies through a built runner, writing each
# result set to /tmp/<name>-<suffix>.json.
#
# Usage: run-tests.sh <runner-path> <suffix> [<platform-suffix>]
#   runner-path       path to a built anyunit-runner.dll (net10), anyunit-browser-wasm.dll
#                      (browser-wasm, via PuppeteerSharp/headless Chrome), or
#                      anyunit-net48-runner-win32.exe / anyunit-net48-runner-win64.exe
#                      (net48, Windows only, one build per bitness) - a .dll is
#                      launched via the dotnet host, anything else (e.g. a
#                      net48 .exe) is invoked directly
#   suffix             tag appended to each output filename, e.g. net10 or net48
#   platform-suffix    optional - passed through as the runner's own
#                      -p/-platform-suffix flag (see AnyUnit.Util.PlatformId),
#                      appended to the *Platform value inside the JSON*, not
#                      just the filename. Needed whenever two calls to this
#                      script would otherwise produce the exact same
#                      auto-detected platform id despite being genuinely
#                      different runs - e.g. the packed anyunit-runner tool
#                      (test-packed-tool) vs the from-source one
#                      (build-and-test), both on the same OS/arch/framework,
#                      or a self-contained single-file publish vs the plain
#                      framework-dependent run on the same machine.
#
# Deliberately does not `set -e` around the individual test runs: the
# runner exits non-zero whenever a test assembly has any Fail/Error
# results, which AnyUnit's self-test assemblies always do, on purpose (see
# WhoTestsTheTesters/ConventionTestProcessor - that's the real pass/fail
# gate, not this script's or the runner's exit code).
set -e

runner_path="$1"
suffix="$2"
platform_suffix="$3"

case "$runner_path" in
  *.dll) runner_cmd=(dotnet "$runner_path") ;;
  *)     runner_cmd=("$runner_path") ;;
esac

platform_args=()
if [ -n "$platform_suffix" ]; then
  platform_args=(-p "$platform_suffix")
fi

run() {
  local dll="WhoTestsTheTesters/Tests/$2/bin/Release/netstandard2.0/$1.dll"
  # Fail loudly, not silently: a missing dll here means the payload
  # assembly never got built (e.g. a project missing from
  # AnyUnit.ci.slnf - confirmed to happen for real: ComboTests/
  # ComboTests.FSharp were missing from it, so this script tried to run
  # them anyway, got a browser-wasm-runner 404/net10-runner file-not-
  # found, and ConventionTestProcessor's own gate never noticed, since
  # it only checks whichever /tmp/*.json files happen to exist, not
  # that all 8 were actually produced). The `|| true` below is only for
  # the runner's own exit code (expected non-zero: these self-test
  # assemblies always contain real Fail/Error cases by design) - it must
  # never also swallow "the assembly wasn't even found".
  if [ ! -f "$dll" ]; then
    echo "run-tests.sh: expected test assembly not found: $dll" >&2
    exit 1
  fi
  local out="/tmp/$1-$suffix.json"
  "${runner_cmd[@]}" run -o "$out" "${platform_args[@]}" "$dll" || true
  # Belt and suspenders alongside the dll check above: the dll existing
  # doesn't guarantee the runner actually got as far as writing its
  # output (a genuine crash mid-run would also leave nothing behind for
  # ConventionTestProcessor's own /tmp/*.json glob to ever see) - same
  # "fail loudly instead of silently disappearing from the gate"
  # reasoning.
  if [ ! -f "$out" ]; then
    echo "run-tests.sh: '$1' produced no output file at $out" >&2
    exit 1
  fi
  # One more rung on the same ladder: the file existing doesn't mean it
  # has anything IN it. A runner that starts, discovers nothing, and
  # writes an empty results file exits 0 and passes every check above -
  # and then ConventionTestProcessor has no results to object to, so the
  # convention gate passes too, and the platform simply vanishes from
  # convention-summary's list with nothing anywhere reporting a problem.
  # That is not hypothetical: it is exactly what the net48 runner did
  # when its embedded-dependency loading produced a second, non-matching
  # AnyUnit identity (see RunTests.RunAlone's NETFRAMEWORK branch) - 16
  # empty result files, every job green. These 8 assemblies always
  # contain tests, so "zero results" is always a bug, never a valid run.
  #
  # The path goes to python3 as its own argv entry, NOT interpolated into
  # the -c script: git-bash's MSYS layer translates a whole POSIX-path
  # argument for a native process, but not one embedded inside a larger
  # string - the same trap that already bit the report-trx step on
  # Windows (see build.yml's own comment there).
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
    echo "run-tests.sh: '$1' produced $out with zero results - the runner ran but discovered no tests" >&2
    exit 1
  fi
}

run BasicTests BasicTests
run ConstraintsTests ConstraintsTests
run NunitTests Style/NunitTests
run XunitTests Style/XunitTests
run FsUnitTests Style/FsUnitTests
run FSharpTests Style/FSharpTests
run ComboTests Style/ComboTests
run ComboTests.FSharp Style/ComboTests.FSharp
