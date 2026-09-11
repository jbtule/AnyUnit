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

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AnyUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// Exception thrown when Assert.All fails one or more items in the collection.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
    public class AllException : AssertException
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AllException"/> class.
        /// </summary>
        /// <param name="totalItems">The total number of items in the collection</param>
        /// <param name="errors">The per-item (index, exception) failures</param>
        public AllException(int totalItems, IEnumerable<KeyValuePair<int, System.Exception>> errors)
            : base(BuildMessage(totalItems, errors.ToList()))
        {
        }

        static string BuildMessage(int totalItems, IList<KeyValuePair<int, System.Exception>> errors)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.CurrentCulture,
                "Assert.All() Failure: {0} out of {1} items in the collection did not pass.", errors.Count, totalItems);

            foreach (var error in errors)
            {
                sb.AppendLine();
                sb.AppendFormat(CultureInfo.CurrentCulture, "[{0}]: {1}", error.Key, error.Value.Message);
            }

            return sb.ToString();
        }
    }
}
