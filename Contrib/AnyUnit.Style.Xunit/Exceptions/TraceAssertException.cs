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

namespace AnyUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception that is thrown when a call to Debug.Assert() fails.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class TraceAssertException : AssertException
    {
        readonly string assertDetailedMessage;
        readonly string assertMessage;

        /// <summary>
        /// Creates a new instance of the <see cref="TraceAssertException"/> class.
        /// </summary>
        /// <param name="assertMessage">The original assert message</param>
        public TraceAssertException(string assertMessage)
            : this(assertMessage, "") { }

        /// <summary>
        /// Creates a new instance of the <see cref="TraceAssertException"/> class.
        /// </summary>
        /// <param name="assertMessage">The original assert message</param>
        /// <param name="assertDetailedMessage">The original assert detailed message</param>
        public TraceAssertException(string assertMessage, string assertDetailedMessage)
        {
            this.assertMessage = assertMessage ?? "";
            this.assertDetailedMessage = assertDetailedMessage ?? "";
        }

        /// <summary>
        /// Gets the original assert detailed message.
        /// </summary>
        public string AssertDetailedMessage
        {
            get { return assertDetailedMessage; }
        }

        /// <summary>
        /// Gets the original assert message.
        /// </summary>
        public string AssertMessage
        {
            get { return assertMessage; }
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message
        {
            get
            {
                string result = "Debug.Assert() Failure";

                if (!String.IsNullOrEmpty(AssertMessage))
                {
                    result += " : " + AssertMessage;

                    if (!String.IsNullOrEmpty(AssertDetailedMessage))
                        result += Environment.NewLine + "Detailed Message:" + Environment.NewLine + AssertDetailedMessage;
                }

                return result;
            }
        }


    }
}