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

using Microsoft.Testing.Extensions.TrxReport.Abstractions;

namespace AnyUnit.TestingPlatform
{
    // Declaring this capability (added to AnyUnitTestFrameworkCapabilities
    // below) is what makes a consuming test project's `--report-trx` flag
    // work at all - MTP only wires up TRX generation for a test framework
    // that says it supports it. The actual .trx file writing is handled
    // entirely by Microsoft.Testing.Extensions.TrxReport (a separate,
    // much larger package that self-registers via a build-time hook), which
    // the consuming test project references itself - see
    // AnyUnit.TestingPlatform's README.md. This class's only real job, beyond the capability
    // declaration itself, is tracking whether MTP actually turned TRX
    // output on (Enable()), so AnyUnitTestFramework knows whether it's
    // worth attaching the richer Trx*Property values to each TestNode.
    internal sealed class AnyUnitTrxReportCapability : ITrxReportCapability
    {
        public bool IsSupported
        {
            get { return true; }
        }

        public bool IsEnabled { get; private set; }

        public void Enable()
        {
            IsEnabled = true;
        }
    }
}
