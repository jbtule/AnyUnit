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
using System.Reflection;
using System.Text;
using AnyUnit.Util;

namespace AnyUnit.Style.Xunit
{

    public class UnlabeledFixtureAttribute:Run.Attributes.TestFixtureAttributeBase
    {
 


        public class UseFixtureParamSet:ParameterSet
        {
             public UseFixtureParamSet(Type useInteface, object fixture) : base(useInteface, fixture)
             {
             }
        }

        public override FixtureInitializer FixtureInit
        {
            get
            {
                return (t, a) =>
                           {
                               var fixture = base.FixtureInit(t, new object[]{});

                               if (a.Length == 2)
                               {
                                   ((Type)a[0]).GetMethod("SetFixture").Invoke(fixture, new[] { a[1] });
                               }

                               return fixture;
                           };
            }
        }

        public override FixtureParameterSetProducer ParameterSets
        {
            get
            {
                return t =>
                {
                    //reflection set IUseFixture
                    var useType = t.GetInterfaces()
                        .FirstOrDefault(it => it.MatchesGenericDef(typeof(IUseFixture<>)));

                    if (useType != null)
                    {
                        var argType = useType.GetGenericArguments().Single();

                        var arg = Activator.CreateInstance(argType);

                        return new[] {new UseFixtureParamSet(useType, arg)};
                    }

                    return base.ParameterSets(t);
                };
            }
        }

        public override IList<string> GetCategories(Type type)
        {
            return (type.GetAttributes<TraitAttribute>()
                        .Where(trait => trait.Name == "Category")
                        .Select(trait => trait.Value)).ToList();
        }

        public override string GetDescription(Type type)
        {
            return (type.GetAttributes<TraitAttribute>(inherit:false)
                           .Where(trait => trait.Name == "DisplayName")
                           .Select(trait => trait.Value)).FirstOrDefault() ?? String.Empty;
        }
    }
}
