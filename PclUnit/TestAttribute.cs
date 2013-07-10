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
using System.Linq;
using System.Reflection;

namespace PclUnit
{
    public delegate IEnumerable<ParameterSet> ParameterSetTestProducer(MethodInfo method);

    public delegate object TestInvoker(MethodInfo method, object target, object[] args);

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public TestAttribute()
        {
            Timeout = -1;
        }

        public Type ParameterSetsTarget { get; set; }

        public string ParameterSetsStaticMethod { get; set; }

        public virtual TestInvoker TestInvoke
        {
            get { return (method, target, args) => method.Invoke(target, args); }
        }

        public virtual ParameterSetTestProducer ParameterSets
        {
            get
            {
                if (String.IsNullOrEmpty(ParameterSetsStaticMethod))
                    return m => new List<ParameterSet>()
                                    {
                                        new ParameterSet()
                                    };

                return
                    m =>
                        {
                            var typeTarget = ParameterSetsTarget ?? m.DeclaringType;

                            var invoker = typeTarget.GetMethod(ParameterSetsStaticMethod,
                                                               BindingFlags.Static | BindingFlags.Public |
                                                               BindingFlags.NonPublic);
                            if (invoker == null)
                                throw new MissingMemberException(
                                    String.Format("Cound not find static member {0} on {1}.", ParameterSetsStaticMethod,
                                                  typeTarget));

                            return (IEnumerable<ParameterSet>)
                                   typeTarget.GetMethod(ParameterSetsStaticMethod, new[] {typeof (MethodInfo)})
                                             .Invoke(typeTarget, new object[] {m});
                        };
            }
        }

        public string Description { get; set; }
        public string Category { get; set; }
        public int Timeout { get; set; }
    }
}