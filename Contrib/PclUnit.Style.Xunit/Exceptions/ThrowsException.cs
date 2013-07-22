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
    /// Exception thrown when code unexpectedly fails to throw an exception.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class ThrowsException : AssertActualExpectedException
    {
        readonly string stackTrace = null;

        /// <summary>
        /// Creates a new instance of the <see cref="ThrowsException"/> class. Call this constructor
        /// when no exception was thrown.
        /// </summary>
        /// <param name="expectedType">The type of the exception that was expected</param>
        public ThrowsException(Type expectedType)
            : this(expectedType, "(No exception was thrown)", null, null) { }

        /// <summary>
        /// Creates a new instance of the <see cref="ThrowsException"/> class. Call this constructor
        /// when an exception of the wrong type was thrown.
        /// </summary>
        /// <param name="expectedType">The type of the exception that was expected</param>
        /// <param name="actual">The actual exception that was thrown</param>
        [SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1", Justification = "This parameter is verified elsewhere.")]
        public ThrowsException(Type expectedType, Exception actual)
            : this(expectedType, actual.GetType().FullName, actual.Message, actual.StackTrace) { }


        /// <summary>
        /// THIS CONSTRUCTOR IS FOR UNIT TESTING PURPOSES ONLY.
        /// </summary>
        protected ThrowsException(Type expected, string actual, string actualMessage, string stackTrace)
            : base(expected,
                   actual + (actualMessage == null ? "" : ": " + actualMessage),
                   "Assert.Throws() Failure")
        {
            this.stackTrace = stackTrace;
        }



    }
}