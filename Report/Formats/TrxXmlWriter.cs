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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using AnyUnit.Run;

namespace AnyUnit.Report.Formats
{
    // Visual Studio / VSTest "TRX" format (TestRun, xmlns
    // http://microsoft.com/schemas/VisualStudio/TeamTest/2010) - what Azure
    // DevOps' "Publish Test Results" task and VS/Rider's result viewers
    // consume. Emits the real subset those consumers need: Results tied to
    // TestDefinitions via TestEntries/TestLists (using the well-known
    // "Results" list id every VSTest-produced .trx uses), plus a
    // ResultSummary.
    internal class TrxXmlWriter : IResultsFormatWriter
    {
        private static readonly XNamespace Ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        private static readonly Guid ResultsTestListId = new Guid("8c84fa94-04c1-424b-9868-57a2d4851a1d");

        // Well-known TRX TestType id for a plain unit test.
        private static readonly Guid UnitTestType = new Guid("13cdc9d9-ddb5-4fa4-a97d-d965ccfc6d4b");

        public void Write(ResultsFile results, Stream output)
        {
            var entries = ResultsModel.Flatten(results).ToList();

            var unitTestResults = new XElement(Ns + "Results");
            var unitTestDefinitions = new XElement(Ns + "TestDefinitions");
            var testEntries = new XElement(Ns + "TestEntries");

            foreach (var entry in entries)
            {
                // Deterministic, not Guid.NewGuid(): re-converting the same
                // results.json twice should produce the same ids.
                var testId = DeterministicGuid(entry.Test.UniqueName + "|" + entry.Result.Platform);
                var executionId = DeterministicGuid(testId + "|execution");

                var unitTestResult = new XElement(Ns + "UnitTestResult",
                    new XAttribute("executionId", executionId),
                    new XAttribute("testId", testId),
                    new XAttribute("testName", entry.DisplayName),
                    new XAttribute("computerName", entry.Result.Platform ?? string.Empty),
                    new XAttribute("duration", FormatDuration(entry.Result.EndTime - entry.Result.StartTime)),
                    new XAttribute("startTime", entry.Result.StartTime.ToString("o")),
                    new XAttribute("endTime", entry.Result.EndTime.ToString("o")),
                    new XAttribute("testType", UnitTestType),
                    new XAttribute("outcome", ToTrxOutcome(entry.Result.Kind)),
                    new XAttribute("testListId", ResultsTestListId));

                // One <Output> per result, assembled rather than branched:
                // a failing test has both an ErrorInfo AND (now) its
                // captured log, which the earlier either/or shape threw
                // away entirely - every failing test's StdOut was lost.
                var trxOutput = new XElement(Ns + "Output");

                if (entry.Result.Kind == ResultKind.Fail || entry.Result.Kind == ResultKind.Error)
                {
                    // `?? Output` throughout: a results.json written before
                    // Message/StackTrace existed has them as null, and must
                    // still convert to something rather than an empty
                    // <Message/>.
                    trxOutput.Add(new XElement(Ns + "ErrorInfo",
                        new XElement(Ns + "Message", entry.Result.Message ?? entry.Result.Output ?? string.Empty),
                        new XElement(Ns + "StackTrace", entry.Result.StackTrace ?? entry.Result.Output ?? string.Empty)));
                }
                else if (entry.Result.Kind == ResultKind.Ignore)
                {
                    // TRX has no skip-reason slot of its own - every real
                    // framework (NUnit, MSTest, xunit.v3) puts it in
                    // ErrorInfo/Message under a NotExecuted outcome, so
                    // that's where it goes. `?? Output` so an older
                    // results.json, which has no SkipReason at all, still
                    // says something rather than nothing.
                    var reason = entry.Result.SkipReason ?? entry.Result.Output;
                    if (!string.IsNullOrEmpty(reason))
                    {
                        trxOutput.Add(new XElement(Ns + "ErrorInfo",
                            new XElement(Ns + "Message", reason)));
                    }
                }

                if (!string.IsNullOrEmpty(entry.Result.Output))
                    trxOutput.Add(new XElement(Ns + "StdOut", entry.Result.Output));

                if (trxOutput.HasElements)
                    unitTestResult.Add(trxOutput);

                unitTestResults.Add(unitTestResult);

                var testMethod = new XElement(Ns + "TestMethod",
                    new XAttribute("codeBase", entry.Assembly.Name ?? string.Empty),
                    new XAttribute("className", ResultsModel.StripPrefix(entry.Fixture.UniqueName)),
                    new XAttribute("name", entry.Test.Name));

                var unitTest = new XElement(Ns + "UnitTest",
                    new XAttribute("name", entry.DisplayName),
                    new XAttribute("storage", entry.Assembly.Name ?? string.Empty),
                    new XAttribute("id", testId));

                // Description, Categories and Properties were all simply
                // never emitted, despite TestMeta having carried Description
                // and Category since long before this writer existed - the
                // TRX schema orders them <Description>, <TestCategory>,
                // <Properties>, then <Execution>/<TestMethod>.
                if (!string.IsNullOrEmpty(entry.Test.Description))
                    unitTest.Add(new XElement(Ns + "Description", entry.Test.Description));

                // Fixture-level categories apply to every test in the
                // fixture, so they're merged in here the same way the MTP
                // adapter merges them for TrxCategoriesProperty.
                var categories = entry.Test.Category
                    .Concat(entry.Fixture.Category)
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .ToList();
                if (categories.Count > 0)
                {
                    unitTest.Add(new XElement(Ns + "TestCategory",
                        categories.Select(c => new XElement(Ns + "TestCategoryItem",
                            new XAttribute("TestCategory", c)))));
                }

                // TRX <Property> is a flat Key/Value pair, so a key with
                // more than one value becomes more than one <Property> -
                // that's what NUnit's and MSTest's own TRX output does too.
                var propertyElements = MergedProperties(entry)
                    .SelectMany(pair => pair.Value.Select(value => new XElement(Ns + "Property",
                        new XElement(Ns + "Key", pair.Key),
                        new XElement(Ns + "Value", value ?? string.Empty))))
                    .ToList();
                if (propertyElements.Count > 0)
                    unitTest.Add(new XElement(Ns + "Properties", propertyElements));

                unitTest.Add(new XElement(Ns + "Execution", new XAttribute("id", executionId)));
                unitTest.Add(testMethod);

                unitTestDefinitions.Add(unitTest);

                testEntries.Add(new XElement(Ns + "TestEntry",
                    new XAttribute("testId", testId),
                    new XAttribute("executionId", executionId),
                    new XAttribute("testListId", ResultsTestListId)));
            }

            var counters = new XElement(Ns + "Counters",
                new XAttribute("total", entries.Count),
                new XAttribute("executed", entries.Count(e => e.Result.Kind != ResultKind.NoError)),
                new XAttribute("passed", entries.Count(e => e.Result.Kind == ResultKind.Success)),
                new XAttribute("failed", entries.Count(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error)),
                new XAttribute("error", entries.Count(e => e.Result.Kind == ResultKind.Error)),
                new XAttribute("notExecuted", entries.Count(e => e.Result.Kind == ResultKind.Ignore || e.Result.Kind == ResultKind.NoError)));

            var overallOutcome = entries.Any(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error)
                ? "Failed"
                : "Completed";

            var start = entries.Count > 0 ? entries.Min(e => e.Result.StartTime) : DateTime.UtcNow;
            var finish = entries.Count > 0 ? entries.Max(e => e.Result.EndTime) : DateTime.UtcNow;

            var testRun = new XElement(Ns + "TestRun",
                new XAttribute("id", DeterministicGuid("AnyUnit.Report|" + start.ToString("o") + "|" + finish.ToString("o"))),
                new XAttribute("name", "AnyUnit.Report convert"),
                new XElement(Ns + "Times",
                    new XAttribute("creation", DateTime.UtcNow.ToString("o")),
                    new XAttribute("start", start.ToString("o")),
                    new XAttribute("finish", finish.ToString("o"))),
                unitTestResults,
                unitTestDefinitions,
                new XElement(Ns + "TestLists",
                    new XElement(Ns + "TestList",
                        new XAttribute("name", "Results"),
                        new XAttribute("id", ResultsTestListId))),
                testEntries,
                new XElement(Ns + "ResultSummary",
                    new XAttribute("outcome", overallOutcome),
                    counters));

            // XmlDocumentSave, not XDocument.Save: a test's own output can
            // contain control characters XML cannot represent - see that
            // class.
            XmlDocumentSave.Save(new XDocument(new XDeclaration("1.0", "UTF-8", null), testRun), output);
        }

        // Fixture-level properties first, then the test's own, so a test
        // that names the same key as its fixture adds to it rather than
        // being hidden by it. Both bags are initialised (never null) by
        // TestMeta/FixtureMeta's constructors, but an older results.json
        // read back through a future reader is still guarded against here.
        private static IDictionary<string, IList<string>> MergedProperties(TestCaseEntry entry)
        {
            var merged = new Dictionary<string, IList<string>>();
            foreach (var source in new[] { entry.Fixture.Properties, entry.Test.Properties })
            {
                if (source == null)
                    continue;
                foreach (var pair in source)
                {
                    IList<string> values;
                    if (!merged.TryGetValue(pair.Key, out values))
                    {
                        values = new List<string>();
                        merged[pair.Key] = values;
                    }
                    foreach (var value in pair.Value ?? new List<string>())
                    {
                        if (!values.Contains(value))
                            values.Add(value);
                    }
                }
            }
            return merged;
        }

        private static string ToTrxOutcome(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Success:
                    return "Passed";
                case ResultKind.Ignore:
                    return "NotExecuted";
                case ResultKind.Fail:
                case ResultKind.Error:
                    return "Failed";
                default:
                    return "NotExecuted";
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;
            return duration.ToString(@"hh\:mm\:ss\.fffffff", CultureInfo.InvariantCulture);
        }

        private static Guid DeterministicGuid(string text)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
                return new Guid(hash);
            }
        }
    }
}
