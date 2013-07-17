using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PclUnit.Util;

namespace PclUnit.Runner
{
    public class TestHarness
    {
        public TestHarness(TestAttribute attribute, MethodInfo method)
        {
            Attribute =attribute;
            Method = method;
            if (attribute != null)
            {
                Category = attribute.Category.SafeSplit(",").ToList();
                Description = attribute.Description;
                Timeout = attribute.Timeout;
            }
        }

        public string Description { get; set; }

        public List<string> Category { get; set; }

        public TestAttribute Attribute { get; set; }

        public MethodInfo Method { get; set; }

        public IEnumerable<ParameterSet> ParameterSets()
        {
            return Attribute.ParameterSets(Method);
        }

        public int Timeout { get; set; }
    }
}