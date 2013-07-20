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

using System.Collections.Generic;
using System.Reflection;
using PclUnit.Run.Attributes;

namespace PclUnit.Run
{
    public class TestHarness
    {
        public TestHarness(TestAttributeBase attribute, MethodInfo method)
        {
            Attribute =attribute;
            Method = method;
            if (attribute != null)
            {
                Category = attribute.GetCategories(method);
                Description = attribute.GetDescription(method);
                Timeout = attribute.GetTimeout(method);
            }
        }

        public string Description { get; set; }

        public IList<string> Category { get; set; }

        public TestAttributeBase Attribute { get; set; }

        public MethodInfo Method { get; set; }

        public IEnumerable<ParameterSet> ParameterSets()
        {
            return Attribute.ParameterSets(Method);
        }

        public int Timeout { get; set; }
    }
}