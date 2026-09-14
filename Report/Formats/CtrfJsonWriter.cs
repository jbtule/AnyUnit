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
                // CTRF has no dedicated skip-reason field, so a skipped
                // test's reason goes in `message` - which this writer
                // couldn't do before, because it nulled `message` for
                // anything that wasn't Fail/Error.
                message = ToMessage(e.Result),
                // `trace` is CTRF's stack-trace field. It was never emitted
                // at all, because there was nothing to put in it that
                // wasn't already in `message`.
                trace = e.Result.StackTrace ?? (IsBad(e.Result) ? e.Result.Output : null),
                // CTRF's own tags/labels: tags is a flat string array
                // (categories), labels a key -> values object. xunit.v3's
                // CTRF output uses exactly this split.
                tags = Tags(e),
                labels = Labels(e),
                extra = e.Result.ExceptionType == null
                    ? null
                    : new { exception = e.Result.ExceptionType },
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
                    environment = entries.Count > 0 ? BuildEnvironment(entries[0].Result) : null,
                    tests,
                },
            };

            JsonSerializer.Serialize(output, document, Options);
        }

        private static bool IsBad(Result result)
        {
            return result.Kind == ResultKind.Fail || result.Kind == ResultKind.Error;
        }

        // `?? Output` on the failure path: a results.json written before
        // Result.Message existed has only Output, and must still produce a
        // message rather than null.
        private static string ToMessage(Result result)
        {
            if (IsBad(result))
                return result.Message ?? result.Output;
            if (result.Kind == ResultKind.Ignore)
                return result.SkipReason ?? result.Output;
            return null;
        }

        private static string[] Tags(TestCaseEntry entry)
        {
            var tags = entry.Test.Category.Concat(entry.Fixture.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToArray();
            return tags.Length > 0 ? tags : null;
        }

        // CTRF's `labels` is specified as key -> scalar-or-array; always
        // emitting the array form means a consumer has one shape to read
        // rather than two. Category is deliberately NOT folded in here -
        // it's `tags` above, which is what a CTRF viewer actually renders
        // as a chip.
        private static IDictionary<string, IList<string>> Labels(TestCaseEntry entry)
        {
            var labels = new Dictionary<string, IList<string>>();
            foreach (var source in new[] { entry.Fixture.Properties, entry.Test.Properties })
            {
                if (source == null)
                    continue;
                foreach (var pair in source)
                {
                    if (pair.Key == "Category")
                        continue;
                    IList<string> values;
                    if (!labels.TryGetValue(pair.Key, out values))
                    {
                        values = new List<string>();
                        labels[pair.Key] = values;
                    }
                    foreach (var value in pair.Value ?? new List<string>())
                    {
                        if (!values.Contains(value))
                            values.Add(value);
                    }
                }
            }
            return labels.Count > 0 ? labels : null;
        }

        // CTRF's environment block is one per report, not per test - a
        // real limitation when converting a multi-input-merged
        // ResultsFile (see AnyUnit.Report's ConvertCommand) that genuinely
        // spans more than one OS/runtime: this reports the FIRST entry's
        // environment as representative, not every one that appears.
        // osPlatform is a coarse guess from OSDescription's free text
        // (there's no structured platform enum captured alongside it -
        // see AnyUnit.Run.Result.SetEnvironment) - close enough for a
        // human/dashboard, not guaranteed to match any specific
        // convention a CTRF viewer might expect.
        private static object BuildEnvironment(Result result)
        {
            return new
            {
                osPlatform = ToOsPlatform(result.OSDescription),
                osRelease = result.OSDescription,
                extra = new { dotnetRuntime = result.FrameworkDescription, architecture = result.OSArchitecture },
            };
        }

        private static string ToOsPlatform(string osDescription)
        {
            if (string.IsNullOrEmpty(osDescription))
                return null;
            if (osDescription.IndexOf("Windows", StringComparison.OrdinalIgnoreCase) >= 0)
                return "windows";
            if (osDescription.IndexOf("Darwin", StringComparison.OrdinalIgnoreCase) >= 0
                || osDescription.IndexOf("Mac", StringComparison.OrdinalIgnoreCase) >= 0)
                return "darwin";
            if (osDescription.IndexOf("Linux", StringComparison.OrdinalIgnoreCase) >= 0)
                return "linux";
            return null;
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
