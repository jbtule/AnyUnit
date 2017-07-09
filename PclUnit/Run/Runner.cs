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
using System.Reflection;
using PclUnit.Run.Attributes;
using PclUnit.Util;

namespace PclUnit.Run
{

    internal class DefaultDiscovery : TestFixtureDiscoveryAttribute
    {
        public override FixtureGenerator Generator
        {
            get
            {
                return a => a.GetExportedTypes()
                             .Select(t => new Fixture(t.GetTypeInfo().GetTopMostCustomAttribute<TestFixtureAttributeBase>(), t))
                             .Where(f => f.Attribute != null);
            }
        }
    }


    public class Runner:IJsonSerialize
    {
        public Runner()
        {
            Assemblies = new List<AssemblyMeta>();
            Tests = new List<Test>();
        }

        protected Runner(string platform):this()
        {
            Platform = platform;

        }

        public string Platform { get; set; }
        public IList<AssemblyMeta> Assemblies { get; set; }
        public IList<Test> Tests { get; set; }

        public void RunAll(Action<Result> resultCallBack, TestFilter testFilter = null)
        {
            testFilter = testFilter ?? new TestFilter();


            foreach (var test in Tests)
            {
                if (!testFilter.ShouldRun(test))
                {
                    test.ParameterSetRelease();
                    continue;
                }
                var result = test.Run(Platform);
                resultCallBack(result);
            }
        }
        
      
        
        public string ToListJson()
        {
            return String.Format("{{Platform:\"{1}\", Assemblies:[{0}]}}",
                          String.Join(",", Assemblies.Select(it => it.ToListJson()).ToArray()), Platform);
        }

        public string ToItemJson()
        {
            return ToListJson();
        }


        public static Runner Create(string platformId, IEnumerable<Assembly> assemblies)
        {
            var runner = new Runner(platformId);

            foreach (var assembly in assemblies)
            {
                var assemblyMeta = new AssemblyMeta(assembly);

                runner.Assemblies.Add(assemblyMeta);

                var generators = assembly.GetCustomAttributes().OfType<TestFixtureDiscoveryAttributeBase>().ToList();

                generators.Add(new DefaultDiscovery());
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