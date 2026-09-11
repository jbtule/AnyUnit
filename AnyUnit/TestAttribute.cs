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
using System.Linq;
using System.Reflection;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;
using AnyUnit.Compat.NetStandardV1;

namespace AnyUnit
{
    public delegate IEnumerable<ParameterSet> TestParameterSetProducer(MethodInfo method);

    public delegate object TestInvoker(IAssertionHelper assetHelper, MethodInfo method, object target, object[] args);

    public class TestAttribute : TestAttributeBase
    {
        public TestAttribute()
        {
            Timeout = -1;
        }

        public Type ParameterMethodSource { get; set; }

        public string ParameterMethod { get; set; }

        public override TestParameterSetProducer ParameterSets
        {
            get
            {
                if (String.IsNullOrEmpty(ParameterMethod))
                    return base.ParameterSets;

                return
                    m =>
                        {
                            var typeTarget = ParameterMethodSource ?? m.DeclaringType;

                            var invoker = typeTarget.GetMethod(ParameterMethod,
                                                               BindingFlags.Static | BindingFlags.Public |
                                                               BindingFlags.NonPublic);
                            if (invoker == null)
                                throw new MissingMemberException(
                                    String.Format("Cound not find static member {0} on {1}.", ParameterMethod,
                                                  typeTarget));

                            return (IEnumerable<ParameterSet>)
                                   typeTarget.GetMethod(ParameterMethod, new[] {typeof (MethodInfo)})
                                             .Invoke(typeTarget, new object[] {m});
                        };
            }
        }

        public override int GetTimeout(MethodInfo method)
        {
            return Timeout;
        }

        public override IList<string> GetCategories(MethodInfo method)
        {
            return Category.SafeSplit(",").ToList();
        }

        public override string GetDescription(MethodInfo method)
        {
            return Description;
        }

        public string Description { get; set; }
        public string Category { get; set; }
        public int Timeout { get; set; }
    }
}