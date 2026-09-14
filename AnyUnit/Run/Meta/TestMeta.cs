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
using AnyUnit.Util;

namespace AnyUnit.Run
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
            // Initialised here, not left null, for the same reason Category
            // is: ResultsFileReader and ConventionTestProcessor both
            // deserialize with PreferredObjectCreationHandling.Populate,
            // which populates this instance rather than assigning a fresh
            // one - so a results.json written before this field existed
            // leaves whatever the constructor made, and that had better be
            // an empty bag rather than null.
            Properties = new Dictionary<string, IList<string>>();

            Results = new CallBackList<Result>(it => it.Test = this);
        }

        public IList<Result> Results { get; set; }

        public FixtureMeta Fixture { get; set; }

 
        public string UniqueName { get; set; }
        public string Name { get; set; }
  
        // Two independent serializers - ToItemJson reverses the field order
        // and inserts the parent, with its own positional numbering, so a
        // new field has to be added to both by hand. Nothing but the
        // round-trip test catches a mismatch.
        public string ToListJson()
        {
            return String.Format("{{\"Name\":\"{0}\", \"UniqueName\":\"{1}\", \"Description\":\"{2}\", \"Category\":{3}, \"Properties\":{6}, \"Timeout\":{4}, \"Results\":[{5}]}}",
                                 Name.EscapeJson(), UniqueName.EscapeJson(), Description.EscapeJson(), Category.ToListJson(),
                                 Timeout.MaybeStruct(m => m.ToString(), () => "null"),
                                 string.Join(",",Results.Select(it => it.ToListJson()).ToArray()),
                                 Properties.ToDictionaryJson());
        }

        public string ToItemJson()
        {
            return String.Format("{{\"Fixture\":{4}, \"Timeout\":{5}, \"Description\":\"{2}\", \"Properties\":{6}, \"Category\":{3}, \"UniqueName\":\"{1}\", \"Name\":\"{0}\"}}",
                                 Name.EscapeJson(), UniqueName.EscapeJson(), Description.EscapeJson(), Category.ToListJson(), Fixture.ToItemJson(),
                                 Timeout.MaybeStruct(m => m.ToString(), () => "null"),
                                 Properties.ToDictionaryJson());

        }

        public int? Timeout { get; set; }
        public string Description { get; set; }
        public IList<string> Category { get; set; }

        // Arbitrary key -> values metadata from the style layer, ALONGSIDE
        // the flat Category list rather than replacing it. Category stays
        // its own field because it's the one key every report format
        // singles out (TRX <TestCategory>, CTRF tags), and folding it into
        // the bag would mean every consumer had to know to fish it back
        // out. Values are a list because a key can legitimately repeat -
        // NUnit's [Property] and xUnit's [Trait] both allow it.
        public IDictionary<string, IList<string>> Properties { get; set; }

    }

    internal class DummyHelper : IAssertionHelper
    {
        public ILog Log { get; set; }
        public IAssert Assert { get; set; }
    }
}