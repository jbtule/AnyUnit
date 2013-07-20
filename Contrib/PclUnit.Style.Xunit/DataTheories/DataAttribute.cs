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
using System.Collections.Generic;
using System.Reflection;

namespace PclUnit.Style.Xunit
{
    /// <summary>
    /// Abstract attribute which represents a data source for a data theory.
    /// Data source providers derive from this attribute and implement GetData
    /// to return the data for the theory.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class DataAttribute : Attribute
    {
        /// <summary>
        /// Returns the data to be used to test the theory.
        /// </summary>
        /// <remarks>
        /// The <paramref name="parameterTypes"/> parameter is provided so that the
        /// test data can be converted to the destination parameter type when necessary.
        /// Generally, data should NOT be automatically converted, UNLESS the source data
        /// format does not have rich types (for example, all numbers in Excel spreadsheets
        /// are returned as <see cref="Double"/> even if they are integers). Derivers of
        /// this class should NOT throw exceptions for mismatched types or mismatched number
        /// of parameters; the test framework will throw these exceptions at the correct
        /// time.
        /// </remarks>
        /// <param name="methodUnderTest">The method that is being tested</param>
        /// <param name="parameterTypes">The types of the parameters for the test method</param>
        /// <returns>The theory data</returns>
        public abstract IEnumerable<object[]> GetData(MethodInfo methodUnderTest, Type[] parameterTypes);

    }
}