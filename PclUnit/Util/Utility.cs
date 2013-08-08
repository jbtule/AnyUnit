// 
//  Copyright 2013 PclUnit Contributors
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

namespace PclUnit.Util
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
        public static string EscapeJson(this string json)
        {
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
