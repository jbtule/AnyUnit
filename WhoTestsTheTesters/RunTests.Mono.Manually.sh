#!/bin/bash
BASEDIR=$(dirname $0)

TESTDLL1="$(BASEDIR)/BasicTests/bin/Debug/BasicTests.dll"
TESTDLL2="$(BASEDIR)/ConstraintsTests/bin/Debug/ConstraintsTests.dll"

mkdir mono-results
mono "$(BASEDIR)/../Runner/Satellites/net/net40/bin/x86/net-40-x86-full.exe" run -o mono-results/net40-x86.json $TESTDLL1 $TESTDLL2
mono "$(BASEDIR)/../Runner/Satellites/net/net45/bin/x86/net-45-x86-full.exe" run -o mono-results/net45-x86.json $TESTDLL1 $TESTDLL2

#we have lots of on purpose failures in this test set
exit 0
