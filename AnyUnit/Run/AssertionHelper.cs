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

namespace AnyUnit.Run
{
    /// <summary>
    /// The seam the engine injects a running test's <see cref="IAssert"/>
    /// and <see cref="ILog"/> through - see Run/Test.cs, which tests a
    /// fixture for this interface and sets both before invoking it.
    /// </summary>
    public interface IAssertionHelper
    {
        ILog Log { get; set; }
        IAssert Assert { get; set; }
    }


    /// <summary>
    /// The base a style's own assertion class derives from
    /// (AnyUnit.Style.Nunit.AssertionHelper, AnyUnit.Style.MsTest's,
    /// AnyUnit.Style.FsUnit's, AnyUnit.Style.Xunit.TestClass).
    /// </summary>
    /// <remarks>
    /// Here rather than in the AnyUnit root because three style packages
    /// export a class of this very name: with both namespaces imported,
    /// C# reported CS0104 and F# silently resolved to whichever was opened
    /// last (#66). AnyUnit.Run is already what a style package imports and
    /// what no ordinary test file needs to.
    /// </remarks>
    public class AssertionHelper:IAssertionHelper
    {
        public ILog Log { get; set; }
        public IAssert Assert { get; set; }
    }
}
