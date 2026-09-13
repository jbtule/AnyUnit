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

using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AnyUnit.Run;

namespace AnyUnit.Report.Formats
{
    // xUnit.net v2 XML report format (assemblies > assembly > collection >
    // test) - what xunit.runner's own -xml output produces, and what a
    // number of CI test-result plugins (Jenkins' xunit plugin, etc.)
    // understand alongside JUnit.
    internal class XUnitXmlWriter : IResultsFormatWriter
    {
        public void Write(ResultsFile results, Stream output)
        {
            var assemblies = new XElement("assemblies");

            foreach (var assembly in results.Assemblies)
            {
                var entries = ResultsModel.Flatten(assembly).ToList();

                var collections = assembly.Fixtures.Select(fixture =>
                {
                    var fixtureEntries = entries.Where(e => e.Fixture == fixture).ToList();

                    var tests = fixtureEntries.Select(entry =>
                    {
                        var test = new XElement("test",
                            new XAttribute("name", ResultsModel.StripPrefix(entry.Test.UniqueName)),
                            new XAttribute("type", ResultsModel.StripPrefix(entry.Fixture.UniqueName)),
                            new XAttribute("method", entry.Test.Name),
                            new XAttribute("time", Seconds(entry.Result)),
                            new XAttribute("result", ToXunitResult(entry.Result.Kind)));

                        if (entry.Result.Kind == ResultKind.Fail || entry.Result.Kind == ResultKind.Error)
                        {
                            test.Add(new XElement("failure",
                                new XAttribute("exception-type", entry.Result.Kind.ToString()),
                                new XElement("message", entry.Result.Output ?? string.Empty)));
                        }
                        else if (entry.Result.Kind == ResultKind.Ignore)
                        {
                            test.Add(new XElement("reason", entry.Result.Output ?? string.Empty));
                        }

                        return test;
                    });

                    return new XElement("collection",
                        new XAttribute("name", fixture.Name ?? string.Empty),
                        new XAttribute("total", fixtureEntries.Count),
                        new XAttribute("passed", fixtureEntries.Count(e => e.Result.Kind == ResultKind.Success)),
                        new XAttribute("failed", fixtureEntries.Count(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error)),
                        new XAttribute("skipped", fixtureEntries.Count(e => e.Result.Kind == ResultKind.Ignore)),
                        new XAttribute("time", TotalSeconds(fixtureEntries)),
                        tests);
                });

                assemblies.Add(new XElement("assembly",
                    new XAttribute("name", assembly.Name ?? string.Empty),
                    new XAttribute("total", entries.Count),
                    new XAttribute("passed", entries.Count(e => e.Result.Kind == ResultKind.Success)),
                    new XAttribute("failed", entries.Count(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error)),
                    new XAttribute("skipped", entries.Count(e => e.Result.Kind == ResultKind.Ignore)),
                    new XAttribute("time", TotalSeconds(entries)),
                    collections));
            }

            new XDocument(new XDeclaration("1.0", "UTF-8", null), assemblies).Save(output);
        }

        private static string ToXunitResult(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Success:
                    return "Pass";
                case ResultKind.Ignore:
                    return "Skip";
                case ResultKind.Fail:
                case ResultKind.Error:
                    return "Fail";
                default:
                    return "Skip";
            }
        }

        private static double TotalSeconds(System.Collections.Generic.IEnumerable<TestCaseEntry> entries)
        {
            var total = 0.0;
            foreach (var entry in entries)
                total += (entry.Result.EndTime - entry.Result.StartTime).TotalSeconds;
            return total;
        }

        private static string Seconds(Result result)
        {
            return (result.EndTime - result.StartTime).TotalSeconds.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}
