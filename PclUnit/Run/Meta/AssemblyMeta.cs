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
using PclUnit.Util;

namespace PclUnit.Run
{
    public class AssemblyMeta : IMeta
    {
        public AssemblyMeta(Assembly assembly):this()
        {
            Name = new AssemblyName(assembly.FullName).Name;
            UniqueName = "A:" + assembly.FullName;
        }


        public AssemblyMeta()
        {
            Fixtures = new CallBackList<FixtureMeta>(it => it.Assembly = this);
        }

        public string UniqueName { get; set; }
        public string Name { get; set; }
    
        public IList<FixtureMeta> Fixtures { get; set; }


        public string ToListJson()
        {
            return String.Format("{{Name:\"{0}\", UniqueName:\"{1}\", Fixtures:[{2}]}}",
                                 Name.EscapeJson(), UniqueName.EscapeJson(),  
                                 String.Join(",", Fixtures.Select(it=>it.ToListJson()).ToArray())
                );
        }

        public string ToItemJson()
        {
            return String.Format("{{UniqueName:\"{1}\",  Name:\"{0}\"}}",
                                 Name.EscapeJson(), UniqueName.EscapeJson());

        }


    }
}