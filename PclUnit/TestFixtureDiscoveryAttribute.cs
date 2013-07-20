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
using System.Text;
using PclUnit.Run;
using PclUnit.Run.Attributes;

namespace PclUnit
{
    public delegate IEnumerable<Fixture> FixtureGenerator(Assembly assembly);

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class TestFixtureDiscoveryAttribute:TestFixtureDiscoveryAttributeBase
    {


        public override FixtureGenerator Generator
        {
            get
            {
                if (String.IsNullOrEmpty(StaticMethodOfGenerator))
                    return a => Enumerable.Empty<Fixture>();

                return
                    a =>
                    {
                        Type typeTarget = TargetOfGenerator;

                        var invoker = typeTarget.GetMethod(StaticMethodOfGenerator,
                                                           BindingFlags.Static | BindingFlags.Public |
                                                           BindingFlags.NonPublic);
                        if (invoker == null)
                            throw new MissingMemberException(
                                String.Format("Cound not find member {0} on {1}.", StaticMethodOfGenerator,
                                              typeTarget));

                        return (IEnumerable<Fixture>)
                               typeTarget.GetMethod(StaticMethodOfGenerator, new[] { typeof(Type) })
                                         .Invoke(typeTarget, new object[] { a });
                    };
            }
        }

        public Type TargetOfGenerator { get; set; }

        public string StaticMethodOfGenerator { get; set; }
    }
}
