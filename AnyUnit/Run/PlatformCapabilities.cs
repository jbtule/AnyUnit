//
//  Copyright 2026 AnyUnit Contributors
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
using AnyUnit.Util;

namespace AnyUnit.Run
{
    /// <summary>
    /// What the platform this process is running on actually provides.
    /// </summary>
    public static class PlatformCapabilities
    {
        /// <summary>
        /// Capabilities available here. Computed once, from the same
        /// single-threaded-runtime probe Test.Run and AsyncTestResult
        /// already branch on, so the three cannot disagree about what this
        /// platform can do; and from the trimmed-framework probe for
        /// <see cref="TestCapabilities.FrameworkReflection"/>.
        /// </summary>
        public static readonly TestCapabilities Available =
            (Utility.IsSingleThreadedRuntime
                ? TestCapabilities.None
                : TestCapabilities.AsyncYield | TestCapabilities.Timeouts | TestCapabilities.Threads)
            | (Utility.IsFrameworkTrimmed
                ? TestCapabilities.None
                : TestCapabilities.FrameworkReflection);

        /// <summary>
        /// The subset of <paramref name="required"/> this platform does not
        /// provide - <see cref="TestCapabilities.None"/> when it provides
        /// all of them.
        /// </summary>
        public static TestCapabilities Missing(TestCapabilities required)
        {
            return required & ~Available;
        }
    }
}
