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
using PclUnit.Run.Attributes;
using PclUnit.Util;

namespace PclUnit
{
    public delegate IEnumerable<ParameterSet> FixtureParameterSetProducer(Type type);

    public delegate object FixtureInitializer(Type type, object[] args);


    public class TestFixtureAttribute : TestFixtureAttributeBase
    {
        public TestFixtureAttribute()
        {
        }


        public Type ParameterMethodSource { get; set; }

        public string ParameterMethod { get; set; }

  

        public override FixtureParameterSetProducer ParameterSets
        {
            get
            {
                if (String.IsNullOrEmpty(ParameterMethod))
                    return base.ParameterSets;

                return
                    m =>
                        {
                            Type typeTarget = (ParameterMethodSource ?? m);

                            var invoker = typeTarget.GetMethod(ParameterMethod,
                                                               BindingFlags.Static | BindingFlags.Public |
                                                               BindingFlags.NonPublic);
                            if (invoker == null)
                                throw new MissingMemberException(
                                    String.Format("Cound not find member {0} on {1}.", ParameterMethod,
                                                  typeTarget));

                            return (IEnumerable<ParameterSet>)
                                   typeTarget.GetMethod(ParameterMethod, new[] {typeof (Type)})
                                             .Invoke(typeTarget, new object[] {m});
                        };
            }
        }

        public override IList<string> GetCategories(Type type)
        {
            return Category.SafeSplit(",").ToList();
        }

        public override string GetDescription(Type type)
        {
            return Description;
        }

        public string Description { get; set; }
        public string Category { get; set; }
    }
}