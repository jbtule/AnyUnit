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
using System.Linq;

namespace PclUnit.Run
{
    public class TestFilter
    {
        private IDictionary<string, bool> _nameFilters;

        public IList<string> Includes { get; set; }
        public IList<string> Excludes { get; set; }


        public TestFilter()
        {
            Includes = new List<string>();
            Excludes = new List<string>();
        }

        public TestFilter(IEnumerable<string> includes, IEnumerable<string> excludes)
        {
            Includes = includes.ToList();
            Excludes = excludes.ToList();
        }

        

        private void Setup()
        {
            if (_nameFilters == null)
            {
                _nameFilters = new Dictionary<string, bool>();
                foreach (var uniqueName in Includes)
                {
                    if (!_nameFilters.ContainsKey(uniqueName))
                        _nameFilters.Add(uniqueName, true);
                }

                foreach (var uniqueName in Excludes)
                {
                    if (!_nameFilters.ContainsKey(uniqueName))
                    {
                        _nameFilters.Add(uniqueName, false);
                    }
                    else
                    {
                        _nameFilters[uniqueName] = false;
                    }
                }
            }

        }


        private string UniqueCategory(string category)
        {
            return string.Format("C:{0}", category);
        }

        public bool ShouldRun(TestMeta test)
        {
            Setup();

            bool? run = null;

          

            if (_nameFilters.ContainsKey(test.UniqueName))
                run = _nameFilters[test.UniqueName];

            if (_nameFilters.ContainsKey(test.Fixture.UniqueName))
                run = _nameFilters[test.Fixture.UniqueName];

            if (_nameFilters.ContainsKey(test.Fixture.Assembly.UniqueName))
                run = _nameFilters[test.Fixture.Assembly.UniqueName];

            if (test.Category.Any(c => _nameFilters.ContainsKey(UniqueCategory(c))))
                run = test.Category
                    .Where(c => _nameFilters.ContainsKey(UniqueCategory(c)))
                    .Select(c=>_nameFilters[UniqueCategory(c)])
                    .All(b=>b);


            if (!run.HasValue)
            {
                if (!_nameFilters.Any() || (!Includes.Any() && Excludes.Any()))
                    return true;
                return false;
            }

            return run.Value;
        }


    }
}