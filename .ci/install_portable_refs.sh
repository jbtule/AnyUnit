#!/bin/sh
echo "Downloading Portable Profiles..."
wget $DL_PORTABLE_REFS > /dev/null 2>&1
unzip NETPortable.zip -d xbuild-frameworks