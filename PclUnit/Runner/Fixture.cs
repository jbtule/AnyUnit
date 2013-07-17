using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PclUnit.Util;

namespace PclUnit.Runner
{
    public class Fixture:FixtureMeta
    {
        public Fixture(TestFixtureAttribute attribute, Type type):base(attribute,type)
        {
            Type = type;
            Attribute = attribute;
        }

        public Type Type { get; set; }

        public TestFixtureAttribute Attribute { get; set; }

        public IEnumerable<ParameterSet> ParameterSets()
        {
            return Attribute.ParameterSets(Type);
        }

        public virtual IEnumerable<TestHarness> GetHarnesses()
        {
            return Type.GetMethods(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
                .Select(m => new TestHarness(m.GetTopMostCustomAttribute<TestAttribute>(), m))
                .Where(th => th.Attribute != null);
        } 
    }
}
