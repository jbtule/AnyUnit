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
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit.Compat.PortableV4;
using AnyUnit.Compat.NetStandardV1;
using System.Threading;
using System.Runtime.InteropServices;

namespace AnyUnit.Util
{
    public class CallBackList<T>:Collection<T>
    {
        private readonly Action<T> _callback;

        public CallBackList(Action<T> callback)
        {
            _callback = callback;
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            _callback(item);
        }
    }


    public static class Utility
    {

        public static IEnumerable<Type> AllTypes(this Assembly asm){
            return asm.GetExportedTypes();
        }

         public static IEnumerable<MethodInfo> AllMethods(this Type type){
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        }

        // The member-lookup helpers below all take a Type the analyzer
        // cannot see annotations for (IL2070). Their callers pass either a
        // type from the rooted test assembly (Fixture.GetFlattenedMethods)
        // or a BCL type kept by a DynamicDependency in AsyncTestResult
        // (Task<T>.Result, ValueTask.AsTask) - see Util/TrimmerAttributes.cs.
        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Trimming.Rooted)]
        public static IEnumerable<MethodInfo> GetFlattenedMethods(this Type type, bool includeNonPublic=false){
            var flags = BindingFlags.Public
                         | BindingFlags.FlattenHierarchy
                         | BindingFlags.Instance 
                         | BindingFlags.Static;
            if(includeNonPublic){
                flags |= BindingFlags.NonPublic;
            }
            return type.GetMethods(flags);
        }

        public static void RunThreadWithState(Action<object> callback, object state){
            ThreadPool.QueueUserWorkItem(d=>callback(d), state);
        }

        // Browser WebAssembly, without <WasmEnableThreads>, has no real
        // background threads: a ThreadPool work item only runs when this
        // thread yields back to the browser's event loop. A caller that
        // blocks synchronously waiting on it (as Test.Run does, to enforce
        // per-test timeouts) would deadlock forever - it never yields, so
        // the queued work item never gets to run.
        public static readonly bool IsSingleThreadedRuntime =
            RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));

        public static bool MatchesGenericDef(this Type type, Type def){
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(def);
        }
        public static bool IsStatic(this Type type)
        {
            return type.GetTypeInfo().IsAbstract && type.GetTypeInfo().IsSealed;
        }

        public static bool IsGeneric(this Type type){
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsValue(this Type type){
            return type.GetTypeInfo().IsValueType;
        }

        public static bool CanBeNull(this Type type){
            return !type.GetTypeInfo().IsValueType || (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().CanAssignFrom(typeof(Nullable<>)));
        }

        // Escapes a string for embedding as a JSON string literal's content
        // (between the surrounding quotes the caller supplies). Only emits
        // the escape sequences the JSON spec (RFC 8259) actually defines -
        // \" \\ \b \f \n \r \t plus \u00XX for other control characters -
        // unlike an earlier version of this method, which also emitted \'
        // for a literal apostrophe. \' is not a valid JSON escape sequence,
        // so any string containing one (e.g. a test name like "the user's
        // session") produced invalid JSON.
        public static string EscapeJson(this string json)
        {
            if (json == null)
                return json;

            var sb = new StringBuilder(json.Length);
            foreach (var c in json)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }


        public static string ToListJson(this IEnumerable<string> target)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            bool first = true;
            foreach (var s in target)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",");
                }
                sb.Append("\"");
                sb.Append(s.EscapeJson());
                sb.Append("\"");
            }
            sb.Append("]");
            return sb.ToString();
        }

        // Either a quoted, escaped JSON string, or the bare literal null.
        // Every pre-existing string field here is written by wrapping the
        // format-string placeholder in literal quotes, which means a null
        // renders as "" and a reader cannot tell "absent" from "empty
        // string". That distinction doesn't matter for the older fields
        // (nobody falls back on them) but it is load-bearing for the
        // Message/StackTrace/ExceptionType/SkipReason fields added in 1.2:
        // every Report/Formats writer does `Message ?? Output`, so a
        // results.json written before those fields existed has to come
        // back as null, not "". So the placeholder for those fields is NOT
        // wrapped in quotes in the format string - this supplies them.
        // Same precedent as Timeout's own
        // `MaybeStruct(m => m.ToString(), () => "null")` in TestMeta.
        public static string ToJsonStringOrNull(this string target)
        {
            if (target == null)
                return "null";
            return "\"" + target.EscapeJson() + "\"";
        }

        // {"key":["v1","v2"], ...} - the shape TestMeta.Properties and
        // FixtureMeta.Properties serialize to. Values are a list, not a
        // scalar, because a key can legitimately repeat (NUnit's
        // [Property] and xUnit's [Trait] both allow it) and because CTRF's
        // own `labels` field is specified as scalar-or-array per key -
        // always emitting the array form means one shape to read rather
        // than two. An empty/null bag emits {} rather than null, matching
        // how Category already emits [] rather than null.
        public static string ToDictionaryJson(this IDictionary<string, IList<string>> target)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            if (target != null)
            {
                bool first = true;
                foreach (var pair in target)
                {
                    if (first)
                        first = false;
                    else
                        sb.Append(",");
                    sb.Append("\"");
                    sb.Append(pair.Key.EscapeJson());
                    sb.Append("\":");
                    sb.Append((pair.Value ?? new List<string>()).ToListJson());
                }
            }
            sb.Append("}");
            return sb.ToString();
        }

        public static IEnumerable<string> SafeSplit(this string target, string delimiter)
        {
            if (string.IsNullOrEmpty(target))
                return Enumerable.Empty<string>();

            return target.Split(new[]{delimiter},StringSplitOptions.RemoveEmptyEntries);

        }

        public static string Name(this Delegate del){
            return del.GetMethodInfo().Name;
        }

        public static bool IsOfType(this Type type, object obj){
            return type.IsInstanceOfType(obj);
        }

        public static IEnumerable<Type> Interfaces(this Type type){
            return type.GetTypeInfo().GetInterfaces();
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Trimming.Rooted)]
        public static PropertyInfo InstanceProperty(this Type type, string name, bool includeNonPublic=false){
            var flags = BindingFlags.Public
                         | BindingFlags.Instance ;
            if(includeNonPublic){
                flags |= BindingFlags.NonPublic;
            }

            return type.GetProperty(name,flags);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Trimming.Rooted)]
         public static PropertyInfo Property(this Type type, string name, bool includeNonPublic=false){
            var flags = BindingFlags.Public
                         | BindingFlags.Instance 
                         | BindingFlags.Static;
            if(includeNonPublic){
                flags |= BindingFlags.NonPublic;
            }

            return type.GetProperty(name,flags);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Trimming.Rooted)]
        public static MethodInfo Method(this Type type, string name, bool includeNonPublic=false){
            var flags = BindingFlags.Public
                         | BindingFlags.Instance 
                         | BindingFlags.Static;
            if(includeNonPublic){
                flags |= BindingFlags.NonPublic;
            }

            return type.GetMethod(name,flags);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Trimming.Rooted)]
          public static MethodInfo Method(this Type type, string name, Type[] paramArray){
            return type.GetMethod(name,paramArray);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = Trimming.Rooted)]
        public static PropertyInfo StaticProperty(this Type type, string name){
            var flags = BindingFlags.Public
                         | BindingFlags.Static 
                         | BindingFlags.FlattenHierarchy;

            return type.GetProperty(name,flags);
        }

        public static Type[] GenericArgs(this Type type){
            return type.GetTypeInfo().GetGenericArguments();
        }
        public static bool CanAssignFrom(this Type type, Type from){
            return type.GetTypeInfo().IsAssignableFrom(from.GetTypeInfo());
        }

        public static IEnumerable<TAttr> GetAttributes<TAttr>(this Type type, bool inherit = true) where TAttr : Attribute {
            return type.GetTypeInfo()
                        .GetCustomAttributes(typeof (TAttr), inherit)
                        .OfType<TAttr>();
        }

        
        public static TAttr GetTopMostCustomAttribute<TAttr>(this Type type) where TAttr : Attribute{
            return Utility.GetTopMostCustomAttribute<TAttr>((MemberInfo)type.GetTypeInfo());
            //Cast to member info for type overloading on 
            //frameworks were GetTypeInfo() returns Type
        }

        public static TAttr GetTopMostCustomAttribute<TAttr>(this MemberInfo type) where TAttr : Attribute
        {
            return type.GetCustomAttributes(typeof(TAttr), false)
                       .OfType<TAttr>()
                       .FirstOrDefault()
                   ?? type.GetCustomAttributes(typeof(TAttr), true)
                          .OfType<TAttr>()
                          .FirstOrDefault();
        }

        public static TR Maybe<T, TR>(this T target, Func<T, TR> func, Func<TR> defaultValue =null)
            where T : class 
        {
            if (target == null)
            {
                if (defaultValue == null)
                {
                    return default(TR);
                }
                return defaultValue();
            }
            return func(target);
        }


        public static TR MaybeStruct<T, TR>(this T? target, Func<T, TR> func, Func<TR> defaultValue = null)
            where T : struct 
        {
            if (target == null)
            {
                if (defaultValue == null)
                {
                    return default(TR);
                }
                return defaultValue();
            }
            return func(target.Value);
        }
    }
}
