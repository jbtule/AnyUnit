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

namespace PclUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception thrown when a test method exceeds the given timeout value
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class TimeoutException : AssertException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="TimeoutException"/> class.
        /// </summary>
        /// <param name="timeout">The timeout value, in milliseconds</param>
        public TimeoutException(long timeout)
            : base(String.Format(CultureInfo.CurrentCulture, "Test execution time exceeded: {0}ms", timeout)) { }


    }
}