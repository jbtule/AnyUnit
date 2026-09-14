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
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Run
{

   

    public class FixtureMeta:IMeta
    {
        
        public AssemblyMeta Assembly { get; set; }
                
        public FixtureMeta()
        {
            Category =new List<string>();
            // Initialised, not left null - see TestMeta's own constructor
            // for why (deserialization Populates this instance rather than
            // replacing it, so an older results.json keeps whatever is
            // here).
            Properties = new Dictionary<string, IList<string>>();
            Tests = new CallBackList<TestMeta>(it=>it.Fixture = this);
        }


        public FixtureMeta(TestFixtureAttributeBase attribute, Type type)
            : this()
        {   
       
            Name = type.Name;
            UniqueName = string.Format("T:{0}.{1}", type.Namespace, type.Name);

            if (attribute != null)
            {
                Description = attribute.GetDescription(type);
                Category = attribute.GetCategories(type);
                // ?? keeps the constructor's empty bag if a style's
                // override hands back null - Category has the same hazard
                // but predates this and is left alone.
                Properties = attribute.GetProperties(type) ?? Properties;
            }

        }

        // Two independent serializers - see TestMeta's matching note.
        public string ToListJson()
        {
            return String.Format("{{\"Name\":\"{0}\", \"UniqueName\":\"{1}\", \"Description\":\"{2}\", \"Category\":{3}, \"Properties\":{5}, \"Tests\":[{4}]}}",
                                 Name.EscapeJson(), UniqueName.EscapeJson(), Description.EscapeJson(), Category.ToListJson(), String.Join(",", Tests.Select(it => it.ToListJson()).ToArray()),
                                 Properties.ToDictionaryJson()
                );
        }

        public string ToItemJson()
        {
            return String.Format("{{\"Assembly\":{4}, \"Category\":{3}, \"Properties\":{5}, \"Description\":\"{2}\", \"UniqueName\":\"{1}\", \"Name\":\"{0}\"}}",
                                 Name.EscapeJson(), UniqueName.EscapeJson(), Description.EscapeJson(), Category.ToListJson(), Assembly.ToItemJson(),
                                 Properties.ToDictionaryJson()
                );

        }



        public string UniqueName { get; set; }
        public string Name { get; set; }


  

        public string Description { get; set; }
        public IList<string> Category { get; set; }

        // Fixture-level key -> values metadata, alongside Category - see
        // TestMeta.Properties for the shape and the reasoning.
        public IDictionary<string, IList<string>> Properties { get; set; }

        public IList<TestMeta> Tests { get; set; }
    }
}