using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PclUnit.Runner;

namespace PclUnit
{

    public class TypeWithFixture
    {
        public TypeWithFixture(Type type, TestFixtureAttribute attribute)
        {
            Type = type;
            Attribute = attribute;
        }

        public Type Type { get; protected set; }
        public TestFixtureAttribute Attribute { get; protected set; }
    }

    public delegate IEnumerable<Fixture> FixtureGenerator(Assembly assembly);

    [AttributeUsage(AttributeTargets.Assembly)]
    public class TestFixtureGeneratorAttribute:Attribute
    {


        public virtual FixtureGenerator Generator
        {
            get
            {
                if (String.IsNullOrEmpty(StaticMethodOfGenerator))
                    return a => Enumerable.Empty<Fixture>();

                return
                    a =>
                    {
                        Type typeTarget = TargetOfGenerator;

                        var invoker = typeTarget.GetMethod(StaticMethodOfGenerator,
                                                           BindingFlags.Static | BindingFlags.Public |
                                                           BindingFlags.NonPublic);
                        if (invoker == null)
                            throw new MissingMemberException(
                                String.Format("Cound not find member {0} on {1}.", StaticMethodOfGenerator,
                                              typeTarget));

                        return (IEnumerable<Fixture>)
                               typeTarget.GetMethod(StaticMethodOfGenerator, new[] { typeof(Type) })
                                         .Invoke(typeTarget, new object[] { a });
                    };
            }
        }

        public Type TargetOfGenerator { get; set; }

        public string StaticMethodOfGenerator { get; set; }
    }
}
