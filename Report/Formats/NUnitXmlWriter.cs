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
    // NUnit3 XML report format (test-run > test-suite[type=Assembly] >
    // test-suite[type=TestFixture] > test-case).
    internal class NUnitXmlWriter : IResultsFormatWriter
    {
        public void Write(ResultsFile results, Stream output)
        {
            var allEntries = ResultsModel.Flatten(results).ToList();

            var assemblySuites = results.Assemblies.Select(assembly =>
            {
                var fixtureSuites = assembly.Fixtures.Select(fixture =>
                {
                    var entries = ResultsModel.Flatten(assembly)
                        .Where(e => e.Fixture == fixture)
                        .ToList();

                    var testCases = entries.Select(entry => BuildTestCase(entry));

                    return BuildSuite("TestFixture", fixture.Name, ResultsModel.StripPrefix(fixture.UniqueName), entries, testCases);
                }).ToList();

                var assemblyEntries = ResultsModel.Flatten(assembly).ToList();
                return BuildSuite("Assembly", assembly.Name, assembly.Name, assemblyEntries, fixtureSuites);
            }).ToList();

            var testRun = new XElement("test-run",
                new XAttribute("id", "1"),
                new XAttribute("testcasecount", allEntries.Count),
                new XAttribute("result", allEntries.Any(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error) ? "Failed" : "Passed"),
                new XAttribute("total", allEntries.Count),
                new XAttribute("passed", allEntries.Count(e => e.Result.Kind == ResultKind.Success)),
                new XAttribute("failed", allEntries.Count(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error)),
                new XAttribute("inconclusive", allEntries.Count(e => e.Result.Kind == ResultKind.NoError)),
                new XAttribute("skipped", allEntries.Count(e => e.Result.Kind == ResultKind.Ignore)),
                assemblySuites);

            new XDocument(new XDeclaration("1.0", "UTF-8", null), testRun).Save(output);
        }

        private static XElement BuildSuite(string type, string name, string fullName, System.Collections.Generic.List<TestCaseEntry> entries, object children)
        {
            return new XElement("test-suite",
                new XAttribute("type", type),
                new XAttribute("name", name ?? string.Empty),
                new XAttribute("fullname", fullName ?? string.Empty),
                new XAttribute("testcasecount", entries.Count),
                new XAttribute("result", entries.Any(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error) ? "Failed" : "Passed"),
                new XAttribute("total", entries.Count),
                new XAttribute("passed", entries.Count(e => e.Result.Kind == ResultKind.Success)),
                new XAttribute("failed", entries.Count(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error)),
                new XAttribute("inconclusive", entries.Count(e => e.Result.Kind == ResultKind.NoError)),
                new XAttribute("skipped", entries.Count(e => e.Result.Kind == ResultKind.Ignore)),
                new XAttribute("duration", TotalSeconds(entries)),
                children);
        }

        private static XElement BuildTestCase(TestCaseEntry entry)
        {
            var testCase = new XElement("test-case",
                new XAttribute("name", entry.DisplayName),
                new XAttribute("fullname", ResultsModel.StripPrefix(entry.Test.UniqueName)),
                new XAttribute("methodname", entry.Test.Name),
                new XAttribute("classname", ResultsModel.StripPrefix(entry.Fixture.UniqueName)),
                new XAttribute("result", ToNUnitResult(entry.Result.Kind)),
                new XAttribute("start-time", entry.Result.StartTime.ToString("o")),
                new XAttribute("end-time", entry.Result.EndTime.ToString("o")),
                new XAttribute("duration", Seconds(entry.Result)),
                new XAttribute("asserts", entry.Result.AssertCount));

            if (entry.Result.Kind == ResultKind.Fail || entry.Result.Kind == ResultKind.Error)
            {
                testCase.Add(new XElement("failure",
                    new XElement("message", entry.Result.Output ?? string.Empty),
                    new XElement("stack-trace", entry.Result.Output ?? string.Empty)));
            }
            else if (entry.Result.Kind == ResultKind.Ignore && !string.IsNullOrEmpty(entry.Result.Output))
            {
                testCase.Add(new XElement("reason", new XElement("message", entry.Result.Output)));
            }

            return testCase;
        }

        private static string ToNUnitResult(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Success:
                    return "Passed";
                case ResultKind.Ignore:
                    return "Skipped";
                case ResultKind.Fail:
                case ResultKind.Error:
                    return "Failed";
                default:
                    return "Inconclusive";
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
