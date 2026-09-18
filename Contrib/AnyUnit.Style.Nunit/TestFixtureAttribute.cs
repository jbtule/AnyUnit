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
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit.Util;

namespace AnyUnit.Style.Nunit
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true,
                    Inherited = true)]
    public class TestFixtureAttribute:Run.Attributes.TestFixtureAttributeBase
    {
        public TestFixtureAttribute(params object[] arguments)
        {
           this.Arguments = arguments;
        }

        public object[] Arguments { get; set; }


        public override FixtureParameterSetProducer ParameterSets
        {
            get
            {
                return type => type.GetAttributes<TestFixtureAttribute>()
                                   .Select(a => new ParameterSet(a.Arguments)).ToList();
            }
        }

        public override FixtureInitializer FixtureInit
        {
            get
            {

                return (type, args) =>
                           {
                               var ignore = type.GetAttributes<IgnoreAttribute>()
                                                .FirstOrDefault();

                               if (ignore != null)
                               {
                                   throw new IgnoreException(ignore.Reason);
                               }

                               var platform = type.GetAttributes<PlatformAttribute>()
                                                .FirstOrDefault();

                               string platformReason;
                               if (platform != null && !platform.IsSupported(out platformReason))
                               {
                                   throw new IgnoreException(platformReason);
                               }

                               return base.FixtureInit(type, args);
                           };
            }
        }

        // IL2067: Activator.CreateInstance on the fixture type.
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = Trimming.Rooted)]
        public override Run.Attributes.FixtureOneTimeSetUpAction OneTimeSetUp
        {
            get
            {
                return type =>
                           {
                               var method = GetOneTimeMethod(type, typeof (OneTimeSetUpAttribute));
                               if (method == null)
                                   return null;

                               // Instance methods run against a plain, parameterless-constructed
                               // instance dedicated to the one-time calls - separate from the
                               // fresh-per-test instances every other test gets. If OneTimeTearDown
                               // is also an instance method, it gets this same instance back (see
                               // Fixture.EnsureOneTimeSetUp's `state` threading).
                               object instance = method.IsStatic ? null : Activator.CreateInstance(type);
                               method.Invoke(instance, null);
                               return instance;
                           };
            }
        }

        // IL2067: Activator.CreateInstance on the fixture type.
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = Trimming.Rooted)]
        public override Run.Attributes.FixtureOneTimeTearDownAction OneTimeTearDown
        {
            get
            {
                return (type, state) =>
                           {
                               var method = GetOneTimeMethod(type, typeof (OneTimeTearDownAttribute));
                               if (method == null)
                                   return;

                               object instance = method.IsStatic ? null : (state ?? Activator.CreateInstance(type));
                               method.Invoke(instance, null);
                           };
            }
        }

        private static MethodInfo GetOneTimeMethod(Type type, Type attributeType)
        {
            return type.GetFlattenedMethods(includeNonPublic: true)
                .FirstOrDefault(m => m.GetCustomAttributes(attributeType, true).Any());
        }

        public override IList<string> GetCategories(Type type)
        {
            var cats =Category.SafeSplit(",").ToList();
            cats.AddRange(type.GetAttributes<CategoryAttribute>()
                .Select(trait => trait.Name));

            return cats;
        }

        public override IDictionary<string, IList<string>> GetProperties(Type type)
        {
            return PropertyAttribute.Collect(type.GetTypeInfo().GetCustomAttributes(typeof(PropertyAttribute), true));
        }

        public override string GetDescription(Type type)
        {
            return Description ?? type
                                        .GetAttributes<DescriptionAttribute>()
                                        .Select(trait => trait.Description)
                                        .FirstOrDefault();
        }  
        
        public string Description { get; set; }

        public string Category { get; set; }
    }
}
