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
    public delegate IEnumerable<ParameterSet> ParameterSetFixtureProducer(Type type);

    public delegate object FixtureInitializer(Type type, object[] args);


    public class TestFixtureAttribute : TestFixtureAttributeBase
    {
        public TestFixtureAttribute()
        {
        }


        public Type TargetOfParameterSet { get; set; }

        public string StaticMethodOfParameterSet { get; set; }

        public override FixtureInitializer FixtureInit
        {
            get { return Activator.CreateInstance; }
        }

        public override ParameterSetFixtureProducer ParameterSets
        {
            get
            {
                if (String.IsNullOrEmpty(StaticMethodOfParameterSet))
                    return m => new List<ParameterSet>()
                                    {
                                        new ParameterSet()
                                    };

                return
                    m =>
                        {
                            Type typeTarget = (TargetOfParameterSet ?? m);

                            var invoker = typeTarget.GetMethod(StaticMethodOfParameterSet,
                                                               BindingFlags.Static | BindingFlags.Public |
                                                               BindingFlags.NonPublic);
                            if (invoker == null)
                                throw new MissingMemberException(
                                    String.Format("Cound not find member {0} on {1}.", StaticMethodOfParameterSet,
                                                  typeTarget));

                            return (IEnumerable<ParameterSet>)
                                   typeTarget.GetMethod(StaticMethodOfParameterSet, new[] {typeof (Type)})
                                             .Invoke(typeTarget, new object[] {m});
                        };
            }
        }

        public override IList<string> GetCategories()
        {
            return Category.SafeSplit(",").ToList();
        }

        public override string GetDescription()
        {
            return Description;
        }

        public string Description { get; set; }
        public string Category { get; set; }
    }
}