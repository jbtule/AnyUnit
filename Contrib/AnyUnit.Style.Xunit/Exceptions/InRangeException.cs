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
using System.Globalization;

namespace AnyUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception thrown when a value is unexpectedly not in the given range.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class InRangeException : AssertException
    {
        readonly string actual;
        readonly string high;
        readonly string low;

        /// <summary>
        /// Creates a new instance of the <see cref="InRangeException"/> class.
        /// </summary>
        /// <param name="actual">The actual object value</param>
        /// <param name="low">The low value of the range</param>
        /// <param name="high">The high value of the range</param>
        public InRangeException(object actual, object low, object high)
            : base("Assert.InRange() Failure")
        {
            this.low = low == null ? null : low.ToString();
            this.high = high == null ? null : high.ToString();
            this.actual = actual == null ? null : actual.ToString();
        }

        /// <summary>
        /// Gets the actual object value
        /// </summary>
        public string Actual
        {
            get { return actual; }
        }

        /// <summary>
        /// Gets the high value of the range
        /// </summary>
        public string High
        {
            get { return high; }
        }

        /// <summary>
        /// Gets the low value of the range
        /// </summary>
        public string Low
        {
            get { return low; }
        }

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        /// <returns>The error message that explains the reason for the exception, or an empty string("").</returns>
        public override string Message
        {
            get
            {
                return String.Format(CultureInfo.CurrentCulture,
                                     "{0}\r\nRange:  ({1} - {2})\r\nActual: {3}",
                                     base.Message, Low, High, Actual ?? "(null)");
            }
        }


    }
}