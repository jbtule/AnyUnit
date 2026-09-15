//
//  Copyright 2026 AnyUnit Contributors
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// NUnit's general key/value metadata attribute - `[Property("Bug",
    /// "1234")]` - with the same three value overloads, and the same
    /// subclassing contract: a custom attribute deriving from this and
    /// calling the protected single-argument constructor gets its
    /// property name from its own class name with "Attribute" removed,
    /// exactly as `class SeverityAttribute : PropertyAttribute` does in
    /// real NUnit. Real suites use that pattern, so it has to work here.
    ///
    /// Written for this repo, not ported: NUnit's own PropertyAttribute is
    /// tied to its IApplyToTest/PropertyBag machinery, none of which
    /// exists here - all this needs to be is a name and a value that
    /// TestAttribute/TestFixtureAttribute.GetProperties can read.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly,
                    AllowMultiple = true, Inherited = true)]
    public class PropertyAttribute : Attribute
    {
        public PropertyAttribute(string propertyName, string propertyValue)
        {
            Name = propertyName;
            Value = propertyValue;
        }

        public PropertyAttribute(string propertyName, int propertyValue)
            : this(propertyName, propertyValue.ToString(CultureInfo.InvariantCulture))
        {
        }

        public PropertyAttribute(string propertyName, double propertyValue)
            : this(propertyName, propertyValue.ToString(CultureInfo.InvariantCulture))
        {
        }

        /// <summary>
        /// For subclasses. The property name is the subclass's own type
        /// name minus a trailing "Attribute" - NUnit's rule, kept so a
        /// suite's custom [Severity(...)] carries over as-is.
        /// </summary>
        protected PropertyAttribute(object propertyValue)
        {
            var typeName = GetType().Name;
            Name = typeName.EndsWith("Attribute", StringComparison.Ordinal)
                ? typeName.Substring(0, typeName.Length - "Attribute".Length)
                : typeName;
            Value = propertyValue == null ? "" : Convert.ToString(propertyValue, CultureInfo.InvariantCulture);
        }

        public string Name { get; private set; }

        public string Value { get; private set; }

        // Shared by TestAttribute and TestFixtureAttribute: every
        // PropertyAttribute on the member, subclasses included, as the
        // key/value bag the schema carries. A list per key, since
        // AllowMultiple means a key can legitimately repeat.
        internal static IDictionary<string, IList<string>> Collect(IEnumerable<object> attributes)
        {
            var properties = new Dictionary<string, IList<string>>(StringComparer.Ordinal);
            foreach (var attribute in attributes)
            {
                var property = attribute as PropertyAttribute;
                if (property == null || string.IsNullOrEmpty(property.Name))
                    continue;
                IList<string> values;
                if (!properties.TryGetValue(property.Name, out values))
                {
                    values = new List<string>();
                    properties[property.Name] = values;
                }
                values.Add(property.Value ?? "");
            }
            return properties;
        }
    }

    /// <summary>
    /// NUnit's [Author("name")] / [Author("name", "email")] - a
    /// PropertyAttribute named "Author", as in NUnit itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly,
                    AllowMultiple = true, Inherited = true)]
    public class AuthorAttribute : PropertyAttribute
    {
        public AuthorAttribute(string name)
            : base("Author", name)
        {
        }

        public AuthorAttribute(string name, string email)
            : base("Author", string.Format(CultureInfo.InvariantCulture, "{0} <{1}>", name, email))
        {
        }
    }
}
