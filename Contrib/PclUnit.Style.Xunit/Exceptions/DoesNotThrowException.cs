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

using System;
using System.Diagnostics.CodeAnalysis;

namespace PclUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception thrown when code unexpectedly throws an exception.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class DoesNotThrowException : AssertActualExpectedException
    {

        /// <summary>
        /// Creates a new instance of the <see cref="DoesNotThrowException"/> class.
        /// </summary>
        /// <param name="actual">Actual exception</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0", Justification = "This parameter is verified elsewhere.")]
        public DoesNotThrowException(Exception actual)
            : base("(No exception)",
                   actual.GetType().FullName + (actual.Message == null ? "" : ": " + actual.Message),
                   "Assert.DoesNotThrow() failure")
        {
        }

        /// <summary>
        /// THIS CONSTRUCTOR IS FOR UNIT TESTING PURPOSES ONLY.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Justification = "This constructor is not meant to be public.")]
        protected DoesNotThrowException(string stackTrace)
            : base("Expected", "Actual", "UserMessage")
        {
        }
    }
}