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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit.Compat.PortableV4;
using AnyUnit.Compat.NetStandardV1;
using System.Threading;

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
            return !type.GetTypeInfo().IsValueType || (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().CanAssign(typeof(Nullable<>)));
        }

        public static string EscapeJson(this string json)
        {
            if (json == null)
                return json;
            json = json.Replace(@"\", @"\\"); 
            json = json.Replace(@"/", @"\/");
            json = json.Replace("\"", "\\\"");
            json = json.Replace(@"'", @"\'");
            return json;
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
                sb.Append(s);
                sb.Append("\"");
            }
            sb.Append("]");
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

        public static bool CanAssign(this Type type, Type from){
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
