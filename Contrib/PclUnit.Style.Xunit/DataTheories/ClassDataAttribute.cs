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
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PclUnit.Style.Xunit
{
    /// <summary>
    /// Provides a data source for a data theory, with the data coming from a class
    /// which must implement IEnumerable&lt;object[]&gt;.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes", Justification = "This attribute is designed as an extensibility point.")]
    public class ClassDataAttribute : DataAttribute
    {
        readonly Type @class;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassDataAttribute"/> class.
        /// </summary>
        /// <param name="class">The class that provides the data.</param>
        public ClassDataAttribute(Type @class)
        {
            this.@class = @class;
        }

        /// <summary>
        /// Gets the type of the class that provides the data.
        /// </summary>
        public Type Class
        {
            get { return @class; }
        }

        /// <inheritdoc/>
        public override IEnumerable<object[]> GetData(MethodInfo methodUnderTest, Type[] parameterTypes)
        {
            return (IEnumerable<object[]>)Activator.CreateInstance(@class);
        }
    }
}