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

namespace PclUnit.Run
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
            return Type.GetMethods(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Static)
                .Select(m => new TestHarness(m.GetTopMostCustomAttribute<TestAttributeBase>(), m))
                .Where(th => th.Attribute != null);
        } 
    }
}
