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

        public class Options
        {
            public Options()
            {
                Exclude = new List<string>();
            }

            
            public IList<string> Exclude { get; set; }
        }


        public static IEnumerable<Test> Tests(PlatformMeta platform, IEnumerable<Assembly> assemblies,
                                                 Options options = null)
        {


            var exclude = new Dictionary<string, string>();
            if (options != null)
            {
                foreach (var category in options.Exclude)
                {
                    exclude.Add(category,category);
                } 
            }

            foreach (var assembly in assemblies)
            {
                var assemblyMeta = new AssemblyMeta(assembly);

                platform.Assemblies.Add(assemblyMeta);

                foreach (var type in assembly.GetTypes())
                {
                     var attr = type.GetTopMostCustomAttribute<TestFixtureAttribute>();

                    if (attr != null)
                    {  
                        //Skip excluded categories
                        if (attr.Category.SafeSplit(",").Any(exclude.ContainsKey))
                            continue;

                        var fixture = new FixtureMeta(attr, type);
                        assemblyMeta.Fixtures.Add(fixture);

                        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
                        {
                            var attr2 = method.GetTopMostCustomAttribute<TestAttribute>();

                            if (attr2 != null)
                            {
                                //Skip excluded categories
                                if (attr2.Category.SafeSplit(",").Any(exclude.ContainsKey))
                                    continue;

                                foreach (var constructorSet in attr.ParameterSets(type))
                                {
                                    foreach (var testSet in attr2.ParameterSets(method))
                                    {
                                        fixture.Tests.Add(new Test(attr2, type, constructorSet, method, testSet));
                                    }
                                }
                               
                            }
                        }

                    }

                }
            }
            return platform.Assemblies
                .SelectMany(it => it.Fixtures)
                .SelectMany(it=>it.Tests)
                .OfType<Test>()
                .ToList();
        }
    }
}
