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
using System.Reflection;
using PclUnit.Util;

namespace PclUnit.Runner
{
    public static class Generate
    {

        internal class DefaultGenerator : TestFixtureGeneratorAttribute
        {
            public override FixtureGenerator Generator
            {
                get
                {
                    return a => a.GetExportedTypes()
                                 .Select(t => new Fixture(t.GetTopMostCustomAttribute<TestFixtureAttribute>(), t))
                                 .Where(f => f.Attribute != null);
                }
            }
        }

        public static Runner Tests(string platformId, IEnumerable<Assembly> assemblies)
        {
            var runner = new Runner(platformId);

            foreach (var assembly in assemblies)
            {
                var assemblyMeta = new AssemblyMeta(assembly);

                runner.Assemblies.Add(assemblyMeta);

                var generators = assembly.GetCustomAttributes(true).OfType<TestFixtureGeneratorAttribute>().ToList();
                generators.Add(new DefaultGenerator());
                foreach (var generator in generators)
                {
                    foreach (var fixture in generator.Generator(assembly))
                    {
                        assemblyMeta.Fixtures.Add(fixture);

                        foreach (var testHarness in fixture.GetHarnesses())
                        {

                            int i = 0;
                            foreach (var constructorSet in fixture.ParameterSets())
                            {
                                int j = 0;
                                constructorSet.Index = i++;
                                foreach (var testSet in testHarness.ParameterSets())
                                {
                                    testSet.Index = j++;
                                    var test = new Test(fixture, constructorSet,
                                                        testHarness, testSet);
                                    fixture.Tests.Add(test);
                                    runner.Tests.Add(test);
                                }
                            }
                        }
                    }
                }
            }

            return runner;
        }
    }
}