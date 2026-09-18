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

// Internal stand-ins for the BCL's trimming attributes, which
// netstandard2.0 does not have. The IL trimmer and the Native AOT
// compiler match these by full name, not by defining assembly (the
// documented route for a netstandard library), so these copies are
// enough for them to act on. Only the constructors and properties
// AnyUnit actually uses are defined.
//
// Why AnyUnit is trim-clean by suppression rather than by annotating
// every Type parameter with DynamicallyAccessedMembers: every Type the
// engine reflects over comes out of Assembly.GetTypes() on a test
// assembly, and AnyUnit.TestingPlatform.targets roots that assembly
// (TrimmerRootAssembly) for exactly this reason - a rooted assembly keeps
// every type and member, so the lookups the analyzer cannot prove are in
// fact guaranteed. Annotating the parameters instead would only move the
// warning to the GetTypes() call, whose result the analyzer can never
// annotate. The Types that do not come from a test assembly are handled
// where they are read: Task<T>.Result and ValueTask.AsTask by a
// DynamicDependency (AsyncTestResult), and FSharp.Core's generic
// Async.StartImmediateAsTask by an rd.xml the TestingPlatform targets
// supply to ILC (see AsyncTestResult.FSharpAsyncToTask). See the
// README's Native AOT section.

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class DynamicDependencyAttribute : Attribute
    {
        public DynamicDependencyAttribute(string memberSignature, Type type)
        {
            MemberSignature = memberSignature;
            Type = type;
        }

        // For a type this assembly cannot name (ValueTask is not in
        // netstandard2.0); the trimmer resolves it in the target app.
        public DynamicDependencyAttribute(string memberSignature, string typeName, string assemblyName)
        {
            MemberSignature = memberSignature;
            TypeName = typeName;
            AssemblyName = assemblyName;
        }

        public string MemberSignature { get; }

        public Type Type { get; }

        public string TypeName { get; }

        public string AssemblyName { get; }
    }

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    internal sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute(string category, string checkId)
        {
            Category = category;
            CheckId = checkId;
        }

        public string Category { get; }

        public string CheckId { get; }

        public string Justification { get; set; }
    }
}

namespace AnyUnit.Util
{
    internal static class Trimming
    {
        // The one justification every suppression in this assembly shares.
        public const string Rooted =
            "The test assembly is rooted by AnyUnit.TestingPlatform.targets (TrimmerRootAssembly), " +
            "which keeps every type and member reflection here can reach - see Util/TrimmerAttributes.cs.";

        // Run/AsyncTestResult.cs's F# Async<'T> bridge - see its own comment.
        public const string FSharpAsync =
            "AnyUnit.TestingPlatform.targets supplies ILC the StartImmediateAsTask instantiations via " +
            "build/FSharpAsync.rd.xml when FSharp.Core is referenced; anything else is a loud Error, not a silent pass.";
    }
}
