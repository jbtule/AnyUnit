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

using System.Collections.Generic;
using AnyUnit.Run;

namespace AnyUnit.Report.Formats
{
    // One flattened (Assembly, Fixture, Test, Result) tuple - every target
    // format's "testcase" ultimately corresponds to one of these. A single
    // TestMeta can carry more than one Result (e.g. the same test run under
    // multiple platforms - see ResultsFile.Add), so this is a fan-out over
    // Test.Results, not a 1:1 walk of the containment tree.
    internal sealed class TestCaseEntry
    {
        public AssemblyMeta Assembly;
        public FixtureMeta Fixture;
        public TestMeta Test;
        public Result Result;

        // Test.Name, unless the same test has more than one Result (multi-
        // platform), in which case the platform is appended so every
        // format's test-identity stays unique within its scope.
        public string DisplayName;
    }

    internal static class ResultsModel
    {
        public static IEnumerable<TestCaseEntry> Flatten(ResultsFile results)
        {
            foreach (var assembly in results.Assemblies)
            {
                foreach (var entry in Flatten(assembly))
                    yield return entry;
            }
        }

        public static IEnumerable<TestCaseEntry> Flatten(AssemblyMeta assembly)
        {
            foreach (var fixture in assembly.Fixtures)
            {
                foreach (var test in fixture.Tests)
                {
                    var multiplePlatforms = test.Results.Count > 1;
                    foreach (var result in test.Results)
                    {
                        yield return new TestCaseEntry
                        {
                            Assembly = assembly,
                            Fixture = fixture,
                            Test = test,
                            Result = result,
                            DisplayName = multiplePlatforms
                                ? string.Format("{0} [{1}]", test.Name, result.Platform)
                                : test.Name,
                        };
                    }
                }
            }
        }

        // FixtureMeta/TestMeta.UniqueName carry a "T:"/"M:" discovery-kind
        // prefix (see FixtureMeta's constructor: "T:{namespace}.{name}") -
        // strip it for a format's "classname"/"fullname" attributes, where
        // the prefix would just be noise.
        public static string StripPrefix(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return uniqueName;
            var colon = uniqueName.IndexOf(':');
            return colon >= 0 && colon < 3 ? uniqueName.Substring(colon + 1) : uniqueName;
        }
    }
}
