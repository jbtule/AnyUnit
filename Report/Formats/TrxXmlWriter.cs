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

                if (entry.Result.Kind == ResultKind.Fail || entry.Result.Kind == ResultKind.Error)
                {
                    unitTestResult.Add(new XElement(Ns + "Output",
                        new XElement(Ns + "ErrorInfo",
                            new XElement(Ns + "Message", entry.Result.Output ?? string.Empty))));
                }
                else if (!string.IsNullOrEmpty(entry.Result.Output))
                {
                    unitTestResult.Add(new XElement(Ns + "Output",
                        new XElement(Ns + "StdOut", entry.Result.Output)));
                }

                unitTestResults.Add(unitTestResult);

                unitTestDefinitions.Add(new XElement(Ns + "UnitTest",
                    new XAttribute("name", entry.DisplayName),
                    new XAttribute("storage", entry.Assembly.Name ?? string.Empty),
                    new XAttribute("id", testId),
                    new XElement(Ns + "Execution", new XAttribute("id", executionId)),
                    new XElement(Ns + "TestMethod",
                        new XAttribute("codeBase", entry.Assembly.Name ?? string.Empty),
                        new XAttribute("className", ResultsModel.StripPrefix(entry.Fixture.UniqueName)),
                        new XAttribute("name", entry.Test.Name))));

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

            new XDocument(new XDeclaration("1.0", "UTF-8", null), testRun).Save(output);
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
