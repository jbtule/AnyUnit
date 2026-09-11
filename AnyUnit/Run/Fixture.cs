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

namespace AnyUnit.Run
{
    public class Fixture:FixtureMeta
    {
        public Fixture(TestFixtureAttributeBase attribute, Type type)
            : base(attribute, type)
        {
            Type = type;
            Attribute = attribute;
        }

        public Type Type { get; set; }

        public TestFixtureAttributeBase Attribute { get; set; }

        public IEnumerable<ParameterSet> ParameterSets()
        {
            return Attribute.ParameterSets(Type);
        }

        public virtual IEnumerable<TestHarness> GetHarnesses()
        {
            return Type.GetFlattenedMethods()
                .Select(m => new TestHarness(m.GetTopMostCustomAttribute<TestAttributeBase>(), m))
                .Where(th => th.Attribute != null);
        } 
    }
}
