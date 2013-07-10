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
using PclUnit.Util;
namespace PclUnit.Runner
{
    public class TestMeta : IMeta
    {
        public static TestMeta FakeTest(string description)
        {
            return new TestMeta
                       {
                           Name = description,
                           UniqueName = description,
                           Fixture = new FixtureMeta()
                                         {
                                             Name = description,
                                             UniqueName = description,
                                             Assembly = new AssemblyMeta()
                                                            {
                                                                Name = description,
                                                                UniqueName = description
                                                            }
                                         }
                       };
        }


        public TestMeta()
        {
            Category = new List<string>();

            Results = new CallBackList<Result>(it => it.Test = this);
        }

        public IList<Result> Results { get; set; }

        public FixtureMeta Fixture { get; set; }

 
        public string UniqueName { get; set; }
        public string Name { get; set; }
  
        public string ToListJson()
        {
            return String.Format("{{Name:\"{0}\", UniqueName:\"{1}\", Description:\"{2}\", Category:{3}, Timeout:{4}, Results:[{5}]}}",
                                 Name, UniqueName, Description, Category.ToListJson(),
                                 Timeout.MaybeStruct(m => m.ToString(), () => "null"), 
                                 string.Join(",",Results.Select(it => it.ToListJson()).ToArray()));
        }

        public string ToItemJson()
        {
            return String.Format("{{Fixture:{4}, Timeout:{5}, Description:\"{2}\", Category:{3}, UniqueName:\"{1}\", Name:\"{0}\", }}",
                                 Name, UniqueName, Description, Category.ToListJson(), Fixture.ToItemJson(),Timeout);

        }

        public int? Timeout { get; set; }
        public string Description { get; set; }
        public IList<string> Category { get; set; }

    }

    internal class DummyHelper : IAssertionHelper
    {
        public ILog Log { get; set; }
        public IAssert Assert { get; set; }
    }
}