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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AnyUnit.Util;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// PlatformAttribute is used to mark a test or fixture as needing to
    /// run only on particular operating system platforms. A test that
    /// doesn't match is ignored, not failed - same as IgnoreAttribute.
    ///
    /// Recognized platform names (case-insensitive): Win/Windows, Linux,
    /// Mac/MacOsx/OSX, and Unix (matches both Linux and Mac). This is a
    /// smaller set than real NUnit's (no per-OS-version qualifiers like
    /// "Win8") - add more as real usage needs them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PlatformAttribute : Attribute
    {
        private static readonly IDictionary<string, OSPlatform> Aliases =
            new Dictionary<string, OSPlatform>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Win", OSPlatform.Windows },
                    { "Windows", OSPlatform.Windows },
                    { "Linux", OSPlatform.Linux },
                    { "Mac", OSPlatform.OSX },
                    { "MacOsx", OSPlatform.OSX },
                    { "OSX", OSPlatform.OSX },
                };

        public PlatformAttribute()
        {
        }

        public PlatformAttribute(string include)
        {
            Include = include;
        }

        /// <summary>
        /// Comma-separated list of platform names the test should run on.
        /// </summary>
        public string Include { get; set; }

        /// <summary>
        /// Comma-separated list of platform names the test should NOT run
        /// on. Checked before Include; if it matches, Include is not
        /// consulted.
        /// </summary>
        public string Exclude { get; set; }

        /// <summary>
        /// Optional reason shown when the test is ignored for not matching
        /// the current platform.
        /// </summary>
        public string Reason { get; set; }

        public bool IsSupported(out string reason)
        {
            if (Exclude.SafeSplit(",").Select(s => s.Trim()).Any(MatchesCurrentPlatform))
            {
                reason = Reason ?? string.Format("Excluded on platform(s): {0}", Exclude);
                return false;
            }

            if (!string.IsNullOrEmpty(Include)
                && !Include.SafeSplit(",").Select(s => s.Trim()).Any(MatchesCurrentPlatform))
            {
                reason = Reason ?? string.Format("Requires platform(s): {0}", Include);
                return false;
            }

            reason = null;
            return true;
        }

        private static bool MatchesCurrentPlatform(string name)
        {
            if (string.Equals(name, "Unix", StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                       || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            }

            OSPlatform platform;
            if (!Aliases.TryGetValue(name, out platform))
            {
                throw new ArgumentException(string.Format("Unrecognized platform name '{0}'.", name));
            }

            return RuntimeInformation.IsOSPlatform(platform);
        }
    }
}
