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
                            testcase.Add(BuildFailure("failure", entry.Result));
                            break;
                        case ResultKind.Error:
                            testcase.Add(BuildFailure("error", entry.Result));
                            break;
                        case ResultKind.Ignore:
                            var skipped = new XElement("skipped");
                            // SkipReason where there is one; otherwise the
                            // old first-line-of-log guess, which is all a
                            // pre-1.2 results.json can offer.
                            var reason = entry.Result.SkipReason ?? FirstLine(entry.Result.Output);
                            if (!string.IsNullOrEmpty(reason))
                                skipped.Add(new XAttribute("message", reason));
                            testcase.Add(skipped);
                            break;
                    }

                    // JUnit's <system-out> is the test's captured log -
                    // distinct from the failure message, and previously
                    // never emitted at all.
                    if (!string.IsNullOrEmpty(entry.Result.Output))
                        testcase.Add(new XElement("system-out", entry.Result.Output));

                    testsuite.Add(testcase);
                }

                testsuites.Add(testsuite);
            }

            new XDocument(new XDeclaration("1.0", "UTF-8", null), testsuites).Save(output);
        }

        // <failure message="…" type="…">body</failure> - message is the
        // short exception message, type the exception's type name (real
        // xUnit's JUnit output emits both), and the body the stack trace.
        // Every one of the three falls back to what this writer used to do
        // when the corresponding field is absent, which is exactly the case
        // for a results.json written before those fields existed.
        private static XElement BuildFailure(string name, Result result)
        {
            var element = new XElement(name,
                new XAttribute("message", result.Message ?? FirstLine(result.Output)));

            if (!string.IsNullOrEmpty(result.ExceptionType))
                element.Add(new XAttribute("type", result.ExceptionType));

            element.Add(result.StackTrace ?? result.Output ?? string.Empty);
            return element;
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

        // Kept, but now only as the fallback for a results.json written
        // before Result.Message/SkipReason existed: back then Output was
        // the test's whole captured log and there was no separate short
        // exception message, so its first line was the least-bad "message"
        // attribute. Delete this once pre-1.2 results files stop mattering.
        private static string FirstLine(string output)
        {
            if (string.IsNullOrEmpty(output))
                return string.Empty;
            var newline = output.IndexOfAny(new[] { '\r', '\n' });
            return newline >= 0 ? output.Substring(0, newline) : output;
        }
    }
}
