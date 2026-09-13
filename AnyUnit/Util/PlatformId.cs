//
//  Copyright 2013 AnyUnit Contributors
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Runtime.InteropServices;

namespace AnyUnit.Util
{
    // A dynamic label for Result.Platform (AnyUnit.Run.Meta.Result),
    // derived from the actual running process via RuntimeInformation -
    // not a hardcoded per-runner-project constant. Every runner (net10-
    // runner, net48-runner, AnyUnit.TestingPlatform's MTP adapter) used to
    // hand-pick its own fixed string instead. Among other problems, that
    // meant net48 built for x86 vs x64 (see build.yml's own
    // -p:Platform=x86/x64 split), or net10-runner across build.yml's whole
    // 6-OS/arch matrix, all reported the exact same Platform value - "net48"
    // or "net10" - indistinguishable in the JSON even though they're
    // genuinely different runs, which collides outright once AnyUnit.Report
    // gained multi-input merging (ResultsFile.Add dedups by Platform
    // equality - two different runs reporting the same Platform string
    // means one silently disappears on merge).
    public static class PlatformId
    {
        // e.g. "net10-osx-arm64", "net48-win-x86".
        public static string Current
        {
            get { return ShortFrameworkName() + "-" + Rid(); }
        }

        // RuntimeInformation.FrameworkDescription is the netstandard2.0-safe
        // subset (RuntimeIdentifier needs netstandard2.1+, and this library
        // ships to net48 too - see below and Result.cs's own
        // SetEnvironment for the identical constraint) - a full string like
        // ".NET 10.0.11" or ".NET Framework 4.8.9310.0"; this extracts the
        // same short form the hardcoded constants it replaces already used
        // ("net10", "net48"). .NET Framework's trailing version components
        // are its specific build number (e.g. "4.8.9310.0"), not a patch
        // version to preserve - Major+Minor alone ("net" + 4 + 8 = "net48")
        // is the real-world-correct short form, same for modern .NET where
        // only Major matters by this repo's own convention ("net10", not
        // "net10.0.11" or "net10.0").
        private static string ShortFrameworkName()
        {
            var description = RuntimeInformation.FrameworkDescription ?? "";

            if (description.StartsWith(".NET Framework ", StringComparison.OrdinalIgnoreCase))
            {
                var version = ParseLeadingVersion(description.Substring(".NET Framework ".Length));
                return version != null ? "net" + version.Major + version.Minor : "net48";
            }

            if (description.StartsWith(".NET ", StringComparison.OrdinalIgnoreCase))
            {
                var version = ParseLeadingVersion(description.Substring(".NET ".Length));
                return version != null ? "net" + version.Major : "net";
            }

            return "unknown";
        }

        private static Version ParseLeadingVersion(string text)
        {
            // FrameworkDescription's version text can trail off with extra
            // words/build metadata - take only the leading "X.Y[.Z[.W]]"-
            // shaped prefix before handing it to Version.Parse.
            var end = 0;
            while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.'))
                end++;

            Version version;
            return Version.TryParse(text.Substring(0, end), out version) ? version : null;
        }

        // Reconstructs a real .NET RID's shape (see
        // https://learn.microsoft.com/dotnet/core/rid-catalog - "win-x64",
        // "osx-arm64", "linux-x64", ...) from portable, netstandard2.0-safe
        // primitives, since RuntimeInformation.RuntimeIdentifier itself
        // needs netstandard2.1+, unavailable to this library (which also
        // ships to net48). Covers the desktop platforms this repo's own
        // runners actually target - not the full RID graph (no distro-
        // qualified RIDs; browser-wasm-runner uses its own platform label,
        // not this one, since its actual test execution happens inside a
        // headless-browser-hosted WASM runtime, not this process).
        private static string Rid()
        {
            return OsName() + "-" + RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
        }

        private static string OsName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";
            // Same custom OSPlatform this library already uses elsewhere
            // for the same runtime (see Utility.IsSingleThreadedRuntime) -
            // "BROWSER" isn't one of the built-in OSPlatform.* statics, but
            // Mono's browser-wasm interpreter does answer true to it.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER")))
                return "browser";
            return "unknown";
        }
    }
}
