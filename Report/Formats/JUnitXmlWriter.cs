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
    // The de facto "JUnit XML" format most CI systems (GitHub Actions test-
    // reporter, GitLab, Jenkins, CircleCI) understand. There is no single
    // canonical schema - this emits the widely-supported subset: one
    // <testsuite> per assembly, one <testcase> per (test, platform) result.
    internal class JUnitXmlWriter : IResultsFormatWriter
    {
        public void Write(ResultsFile results, Stream output)
        {
            var testsuites = new XElement("testsuites");

            foreach (var assembly in results.Assemblies)
            {
                var entries = ResultsModel.Flatten(new ResultsFile { Assemblies = { assembly } }).ToList();

                var testsuite = new XElement("testsuite",
                    new XAttribute("name", assembly.Name ?? string.Empty),
                    new XAttribute("tests", entries.Count),
                    new XAttribute("failures", entries.Count(e => e.Result.Kind == ResultKind.Fail)),
                    new XAttribute("errors", entries.Count(e => e.Result.Kind == ResultKind.Error)),
                    new XAttribute("skipped", entries.Count(e => e.Result.Kind == ResultKind.Ignore)),
                    new XAttribute("time", TotalSeconds(entries)));

                foreach (var entry in entries)
                {
                    var testcase = new XElement("testcase",
                        new XAttribute("classname", ResultsModel.StripPrefix(entry.Fixture.UniqueName)),
                        new XAttribute("name", entry.DisplayName),
                        new XAttribute("time", Seconds(entry.Result)));

                    switch (entry.Result.Kind)
                    {
                        case ResultKind.Fail:
                            testcase.Add(new XElement("failure",
                                new XAttribute("message", FirstLine(entry.Result.Output)),
                                entry.Result.Output ?? string.Empty));
                            break;
                        case ResultKind.Error:
                            testcase.Add(new XElement("error",
                                new XAttribute("message", FirstLine(entry.Result.Output)),
                                entry.Result.Output ?? string.Empty));
                            break;
                        case ResultKind.Ignore:
                            var skipped = new XElement("skipped");
                            if (!string.IsNullOrEmpty(entry.Result.Output))
                                skipped.Add(new XAttribute("message", FirstLine(entry.Result.Output)));
                            testcase.Add(skipped);
                            break;
                    }

                    testsuite.Add(testcase);
                }

                testsuites.Add(testsuite);
            }

            new XDocument(new XDeclaration("1.0", "UTF-8", null), testsuites).Save(output);
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

        // AnyUnit's Result.Output is the test's whole captured log, not a
        // separate short exception message - use its first line as the
        // failure/error "message" attribute (full text still goes in the
        // element body) so it reads sensibly in a CI summary view.
        private static string FirstLine(string output)
        {
            if (string.IsNullOrEmpty(output))
                return string.Empty;
            var newline = output.IndexOfAny(new[] { '\r', '\n' });
            return newline >= 0 ? output.Substring(0, newline) : output;
        }
    }
}
