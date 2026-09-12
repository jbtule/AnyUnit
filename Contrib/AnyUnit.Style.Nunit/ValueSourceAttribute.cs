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

using System;
using System.Collections;
using System.Reflection;
using AnyUnit.Run.Attributes;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// ValueSourceAttribute indicates the source to be used to
    /// provide data for one parameter of a test method. The source
    /// may be a static field, property or method on the declaring
    /// type (the default) or on a specified type, and must return
    /// (or evaluate to) an IEnumerable.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class ValueSourceAttribute : ParameterDataAttribute, IArgParameter
    {
        private readonly Type _sourceType;
        private readonly string _sourceName;

        /// <summary>
        /// Construct with the name of a static member (field, property or
        /// method) on the parameter's own declaring type.
        /// </summary>
        public ValueSourceAttribute(string sourceName)
        {
            _sourceName = sourceName;
        }

        /// <summary>
        /// Construct with a type and the name of a static member (field,
        /// property or method) on that type.
        /// </summary>
        public ValueSourceAttribute(Type sourceType, string sourceName)
        {
            _sourceType = sourceType;
            _sourceName = sourceName;
        }

        public override IEnumerable GetData(ParameterInfo parameter)
        {
            var type = _sourceType ?? parameter.Member.DeclaringType;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var field = type.GetField(_sourceName, flags);
            if (field != null)
            {
                return (IEnumerable)field.GetValue(null);
            }

            var property = type.GetProperty(_sourceName, flags);
            if (property != null)
            {
                return (IEnumerable)property.GetValue(null, null);
            }

            var method = type.GetMethod(_sourceName, flags, null, Type.EmptyTypes, null);
            if (method != null)
            {
                return (IEnumerable)method.Invoke(null, null);
            }

            throw new MissingMemberException(
                string.Format("Could not find a static field, property or parameterless method named {0} on {1}.",
                              _sourceName, type));
        }
    }
}
