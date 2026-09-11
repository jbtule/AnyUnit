#!/bin/bash
# Runs all 5 self-test assemblies through a built runner, writing each
# result set to /tmp/<name>-<suffix>.json.
#
# Usage: run-tests.sh <runner-path> <suffix>
#   runner-path  path to a built anyunit-runner.dll (net10), anyunit-wasm.dll
#                (browser-wasm, via PuppeteerSharp/headless Chrome), or
#                anyunit-net48-win-runner-x86.exe / anyunit-net48-win-runner-x64.exe
#                (net48, Windows only, one build per bitness) - a .dll is
#                launched via the dotnet host, anything else (e.g. a
#                net48 .exe) is invoked directly
#   suffix       tag appended to each output filename, e.g. net10 or net48
#
# Deliberately does not `set -e` around the individual test runs: the
# runner exits non-zero whenever a test assembly has any Fail/Error
# results, which AnyUnit's self-test assemblies always do, on purpose (see
# WhoTestsTheTesters/ConventionTestProcessor - that's the real pass/fail
# gate, not this script's or the runner's exit code).
set -e

runner_path="$1"
suffix="$2"

case "$runner_path" in
  *.dll) runner_cmd=(dotnet "$runner_path") ;;
  *)     runner_cmd=("$runner_path") ;;
esac

run() {
  "${runner_cmd[@]}" run -o "/tmp/$1-$suffix.json" "WhoTestsTheTesters/Tests/$2/bin/Release/netstandard2.0/$1.dll" || true
}

run BasicTests BasicTests
run ConstraintsTests ConstraintsTests
run NunitTests Style/NunitTests
run XunitTests Style/XunitTests
run FsUnitTests Style/FsUnitTests
