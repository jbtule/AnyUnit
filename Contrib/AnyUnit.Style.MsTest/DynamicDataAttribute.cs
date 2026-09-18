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
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Style.MsTest
{
    /// <summary>How a [DynamicData] source is named.</summary>
    public enum DynamicDataSourceType
    {
        Property = 0,
        Method = 1,
    }

    /// <summary>
    /// Rows supplied indirectly, by a static property or method returning
    /// IEnumerable&lt;object[]&gt;.
    ///
    /// Implements AnyUnit.Run.Attributes.IGeneratingParameter, the
    /// by-indirection counterpart to IRowInlineParameter, so - exactly as
    /// with [DataRow] - a [DynamicData] source can feed an xUnit [Theory]
    /// or an NUnit [Test] in another assembly with no reference between the
    /// style packages.
    ///
    /// MSTest's default source type is Property, and the declaring type
    /// defaults to the test class - both matched here, so a ported
    /// [DynamicData(nameof(Rows))] behaves identically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DynamicDataAttribute : Attribute, IGeneratingParameter
    {
        public DynamicDataAttribute(string dynamicDataSourceName)
            : this(dynamicDataSourceName, DynamicDataSourceType.Property)
        {
        }

        public DynamicDataAttribute(string dynamicDataSourceName, DynamicDataSourceType dynamicDataSourceType)
        {
            DynamicDataSourceName = dynamicDataSourceName;
            DynamicDataSourceType = dynamicDataSourceType;
        }

        public DynamicDataAttribute(string dynamicDataSourceName, Type dynamicDataDeclaringType)
            : this(dynamicDataSourceName, dynamicDataDeclaringType, DynamicDataSourceType.Property)
        {
        }

        public DynamicDataAttribute(string dynamicDataSourceName, Type dynamicDataDeclaringType,
                                    DynamicDataSourceType dynamicDataSourceType)
            : this(dynamicDataSourceName, dynamicDataSourceType)
        {
            DynamicDataDeclaringType = dynamicDataDeclaringType;
        }

        public string DynamicDataSourceName { get; private set; }

        public DynamicDataSourceType DynamicDataSourceType { get; private set; }

        /// <summary>Null means "the test class itself", as in real MSTest.</summary>
        public Type DynamicDataDeclaringType { get; private set; }

        // IL2075: the data-source member lives on a type in the test assembly.
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = Trimming.Rooted)]
        public IEnumerable<object[]> GetData(MethodInfo method, Type[] parameterTypes)
        {
            if (method == null)
                throw new ArgumentNullException("method");

            var type = DynamicDataDeclaringType ?? method.DeclaringType;

            object raw;
            if (DynamicDataSourceType == DynamicDataSourceType.Method)
            {
                var source = type.GetTypeInfo().GetDeclaredMethod(DynamicDataSourceName);
                if (source == null || !source.IsStatic || source.GetParameters().Length != 0)
                    throw new ArgumentException(string.Format(
                        "Could not find public static parameterless method {0} on {1}",
                        DynamicDataSourceName, type.FullName));
                raw = source.Invoke(null, null);
            }
            else
            {
                var source = type.GetTypeInfo().GetDeclaredProperty(DynamicDataSourceName);
                if (source == null || source.GetMethod == null || !source.GetMethod.IsStatic)
                    throw new ArgumentException(string.Format(
                        "Could not find public static property {0} on {1}",
                        DynamicDataSourceName, type.FullName));
                raw = source.GetValue(null, null);
            }

            // A source that legitimately yields nothing returns an empty
            // sequence rather than null, so the caller's list simply gains
            // no rows. Returning null here instead would look identical to
            // "this attribute produced nothing" but would NRE on the
            // SelectMany in TestMethodAttribute.ParameterSets.
            if (raw == null)
                return Enumerable.Empty<object[]>();

            var rows = raw as IEnumerable<object[]>;
            if (rows == null)
                throw new ArgumentException(string.Format(
                    "{0} on {1} did not return IEnumerable<object[]>",
                    DynamicDataSourceName, type.FullName));

            return rows;
        }
    }
}
