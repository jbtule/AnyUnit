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

namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// Internal stand-in for the BCL's DynamicDependencyAttribute, which
    /// netstandard2.0 does not have. The trimmer and the Native AOT
    /// compiler match this attribute by its full name, not by which
    /// assembly defines it (the documented route for a netstandard
    /// library), so this copy is enough to tell them "keep this member,
    /// it is reached by reflection" - see AsyncTestResult.TaskValue for
    /// the one place that needs it. Only the (memberSignature, type)
    /// constructor is defined; nothing here uses the others.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class DynamicDependencyAttribute : Attribute
    {
        public DynamicDependencyAttribute(string memberSignature, Type type)
        {
            MemberSignature = memberSignature;
            Type = type;
        }

        public string MemberSignature { get; }

        public Type Type { get; }
    }
}
