/*
 * Copyright 2013 Outercurve Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Diagnostics.CodeAnalysis;

namespace PclUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception thrown when two object references are unexpectedly not the same instance.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class SameException : AssertActualExpectedException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SameException"/> class.
        /// </summary>
        /// <param name="expected">The expected object reference</param>
        /// <param name="actual">The actual object reference</param>
        public SameException(object expected,
                             object actual)
            : base(expected, actual, "Assert.Same() Failure") { }

    }
}