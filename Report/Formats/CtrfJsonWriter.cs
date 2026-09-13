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
using System.IO;
using System.Linq;
using System.Text.Json;
using AnyUnit.Run;

namespace AnyUnit.Report.Formats
{
    // Common Test Report Format (CTRF, https://ctrf.io) - a JSON-based,
    // tool-agnostic report aimed at CI summaries/dashboards (GitHub Actions
    // job summaries, third-party CTRF viewers). Unlike the other three
    // writers this is JSON, not XML, so it's built directly with
    // System.Text.Json rather than System.Xml.Linq.
    internal class CtrfJsonWriter : IResultsFormatWriter
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public void Write(ResultsFile results, Stream output)
        {
            var entries = ResultsModel.Flatten(results).ToList();

            var tests = entries.Select(e => new
            {
                name = e.DisplayName,
                status = ToCtrfStatus(e.Result.Kind),
                duration = (long)(e.Result.EndTime - e.Result.StartTime).TotalMilliseconds,
                start = ToEpochMillis(e.Result.StartTime),
                stop = ToEpochMillis(e.Result.EndTime),
                suite = ResultsModel.StripPrefix(e.Fixture.UniqueName),
                message = e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error
                    ? e.Result.Output
                    : null,
            }).ToArray();

            var document = new
            {
                results = new
                {
                    tool = new { name = "AnyUnit" },
                    summary = new
                    {
                        tests = entries.Count,
                        passed = entries.Count(e => e.Result.Kind == ResultKind.Success),
                        failed = entries.Count(e => e.Result.Kind == ResultKind.Fail || e.Result.Kind == ResultKind.Error),
                        pending = 0,
                        skipped = entries.Count(e => e.Result.Kind == ResultKind.Ignore),
                        other = entries.Count(e => e.Result.Kind == ResultKind.NoError),
                        start = entries.Count > 0 ? entries.Min(e => ToEpochMillis(e.Result.StartTime)) : 0,
                        stop = entries.Count > 0 ? entries.Max(e => ToEpochMillis(e.Result.EndTime)) : 0,
                    },
                    tests,
                },
            };

            JsonSerializer.Serialize(output, document, Options);
        }

        // CTRF's status enum is passed|failed|skipped|pending|other. AnyUnit
        // has no CTRF-style "pending" concept; ResultKind.NoError (a test
        // that ran but asserted nothing) maps to "other" rather than
        // "passed"/"failed", since it's neither.
        private static string ToCtrfStatus(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Success:
                    return "passed";
                case ResultKind.Fail:
                case ResultKind.Error:
                    return "failed";
                case ResultKind.Ignore:
                    return "skipped";
                default:
                    return "other";
            }
        }

        private static long ToEpochMillis(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }
    }
}
