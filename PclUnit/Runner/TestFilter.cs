using System.Collections.Generic;
using System.Linq;

namespace PclUnit.Runner
{
    public class TestFilter
    {
        private IDictionary<string, bool> _nameFilters = new Dictionary<string, bool>();

        public static TestFilter Create(IEnumerable<string> uniqueNames)
        {
            var filter = new TestFilter();
            filter.IncludeUniqueNames(uniqueNames);
            return filter;
        }

        public void IncludeUniqueNames(IEnumerable<string> uniqueNames)
        {
            if (uniqueNames != null)
            {
                foreach (var uniqueName in uniqueNames)
                {
                    if (!_nameFilters.ContainsKey(uniqueName))
                        _nameFilters.Add(uniqueName, true);
                }
            }
        }

        public bool ShouldRun(TestMeta test)
        {
            if (!_nameFilters.Any())
                return true;

            if (_nameFilters.ContainsKey(test.UniqueName))
                return true;

            if (_nameFilters.ContainsKey(test.Fixture.UniqueName))
                return true;

            if (_nameFilters.ContainsKey(test.Fixture.Assembly.UniqueName))
                return true;

            return false;
        }


    }
}