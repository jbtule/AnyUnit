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
    /// Exception thrown when the collection did not contain exactly one element.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]

    public class SingleException : AssertException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SingleException"/> class.
        /// </summary>
        /// <param name="count">The numbers of items in the collection.</param>
        public SingleException(int count)
            : base(String.Format(CultureInfo.CurrentCulture, "The collection contained {0} elements instead of 1.", count)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleException"/> class.
        /// </summary>
        /// <param name="count">The numbers of items in the collection.</param>
        /// <param name="expected">The object expected to be in the collection.</param>
        public SingleException(int count, object expected)
            : base(String.Format(CultureInfo.CurrentCulture, "The collection contained {0} instances of '{1}' instead of 1.", count, expected)) { }

    }
}
