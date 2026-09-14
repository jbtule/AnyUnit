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
using System.Text;
using AnyUnit.Run;

namespace AnyUnit.Report.Formats
{
    // GitHub Flavored Markdown, shaped for a GitHub Actions job summary
    // (see README's "Markdown summary" section for the workflow snippet).
    //
    // Unlike the HTML writer, this one cannot be exhaustive, and the
    // constraint is sharper than it looks: a job summary is capped at 1 MiB
    // per step, and going over doesn't fail the step - the upload fails
    // with an error annotation while the job still reports success, so an
    // oversized summary silently doesn't appear at all. A report that only
    // renders for small suites would be worse than useless, so everything
    // here is bounded by construction rather than by hoping: a fixed-size
    // header, a table whose row count is the platform count (small by
    // nature), a capped number of failure entries, a capped output excerpt
    // per entry, and finally a whole-document byte budget as a backstop
    // against the one thing per-item caps can't bound - a single
    // pathologically long line.
    //
    // The full detail lives in the HTML report (-f html), which has no such
    // ceiling; this is deliberately the "what happened, do I need to look?"
    // view, and says so when it truncates.
    internal class MarkdownWriter : IResultsFormatWriter
    {
        // Individually these keep a typical suite's summary to a few KB.
        private const int MaxFailureEntries = 50;
        private const int MaxOutputLines = 20;
        private const int MaxOutputChars = 2000;

        // Backstop only - well under GitHub's 1 MiB so there's room for the
        // truncation notice, and for a runner that counts bytes slightly
        // differently than we do.
        private const int ByteBudget = 750 * 1024;

        public void Write(ResultsFile results, Stream output)
        {
            var all = results.Results.ToList();
            var platforms = all
                .Select(r => r.Platform ?? "")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("## AnyUnit test results");
            sb.AppendLine();

            AppendTotals(sb, results, all, platforms);
            if (platforms.Count > 1)
                AppendPlatformTable(sb, results, all, platforms);

            var truncatedEntries = AppendFailures(sb, results, platforms.Count);

            var text = sb.ToString();
            var budgeted = ApplyByteBudget(text);
            if (budgeted != null)
                text = budgeted;
            else if (truncatedEntries > 0)
            {
                text += string.Format(CultureInfo.InvariantCulture,
                    "{0}_… and {1} more failing test{2}. Full detail in the HTML report (`-f html`)._{0}",
                    Environment.NewLine, truncatedEntries, truncatedEntries == 1 ? "" : "s");
            }

            using (var w = new StreamWriter(output, new UTF8Encoding(false), 4096, leaveOpen: true))
                w.Write(text);
        }

        private static void AppendTotals(StringBuilder sb, ResultsFile results, IList<Result> all, IList<string> platforms)
        {
            var tests = results.Assemblies.SelectMany(a => a.Fixtures).SelectMany(f => f.Tests).Count();

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "**{0}** test{1} across **{2}** platform{3} — **{4}** result{5}",
                tests, tests == 1 ? "" : "s",
                platforms.Count, platforms.Count == 1 ? "" : "s",
                all.Count, all.Count == 1 ? "" : "s"));
            sb.AppendLine();

            sb.AppendLine("| ✅ Passed | ❌ Failed | ⚠️ Errored | ⊘ Ignored | ∅ No assert |");
            sb.AppendLine("|---|---|---|---|---|");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2} | {3} | {4} |",
                all.Count(r => r.Kind == ResultKind.Success),
                all.Count(r => r.Kind == ResultKind.Fail),
                all.Count(r => r.Kind == ResultKind.Error),
                all.Count(r => r.Kind == ResultKind.Ignore),
                all.Count(r => r.Kind == ResultKind.NoError)));
            sb.AppendLine();
        }

        // One row per platform - the one table here that's safe to render in
        // full, because its height is the platform count (a handful, not a
        // function of suite size). This is the multi-platform view the
        // flattened formats can't express, at a size a job summary can
        // always afford.
        private static void AppendPlatformTable(StringBuilder sb, ResultsFile results, IList<Result> all, IList<string> platforms)
        {
            var tests = results.Assemblies.SelectMany(a => a.Fixtures).SelectMany(f => f.Tests).Count();

            sb.AppendLine("| Platform | ✅ | ❌ | ⚠️ | ⊘ | ∅ | Not run |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var platform in platforms)
            {
                var forPlatform = all.Where(r => string.Equals(r.Platform ?? "", platform, StringComparison.Ordinal)).ToList();
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| `{0}` | {1} | {2} | {3} | {4} | {5} | {6} |",
                    EscapeCell(platform),
                    forPlatform.Count(r => r.Kind == ResultKind.Success),
                    forPlatform.Count(r => r.Kind == ResultKind.Fail),
                    forPlatform.Count(r => r.Kind == ResultKind.Error),
                    forPlatform.Count(r => r.Kind == ResultKind.Ignore),
                    forPlatform.Count(r => r.Kind == ResultKind.NoError),
                    Math.Max(0, tests - forPlatform.Count)));
            }
            sb.AppendLine();
        }

        // Returns how many failing tests were left out of the document.
        private static int AppendFailures(StringBuilder sb, ResultsFile results, int platformCount)
        {
            // Grouped by test, not by (test, platform): a test failing on
            // every one of 18 platforms is one problem to look at, not 18,
            // and listing it 18 times is exactly how a summary blows its
            // budget while telling you less.
            var failing = results.Assemblies
                .SelectMany(a => a.Fixtures)
                .SelectMany(f => f.Tests)
                .Select(t => new
                {
                    Test = t,
                    Bad = t.Results.Where(r => r.Kind == ResultKind.Fail || r.Kind == ResultKind.Error)
                                   .OrderBy(r => r.Platform ?? "", StringComparer.Ordinal)
                                   .ToList(),
                })
                .Where(x => x.Bad.Count > 0)
                .ToList();

            if (failing.Count == 0)
            {
                sb.AppendLine("✅ **No failures.**");
                sb.AppendLine();
                return 0;
            }

            sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "### ❌ {0} failing test{1}", failing.Count, failing.Count == 1 ? "" : "s"));
            sb.AppendLine();

            foreach (var entry in failing.Take(MaxFailureEntries))
            {
                var test = entry.Test;
                var name = string.Format("{0}.{1}", StripPrefixSafe(test.Fixture), test.Name);

                // "failed on 2 of 18 platforms" is the compact form of the
                // HTML report's matrix row: for a multi-platform run, which
                // platforms a test fails on is usually the whole diagnosis.
                var everywhere = entry.Bad.Count >= platformCount;
                var scope = platformCount > 1
                    ? (everywhere
                        ? string.Format(CultureInfo.InvariantCulture, " — failed on all {0} platforms", platformCount)
                        : string.Format(CultureInfo.InvariantCulture, " — failed on {0} of {1} platforms",
                            entry.Bad.Count, platformCount))
                    : "";

                sb.AppendLine("<details>");
                sb.AppendLine(string.Format("<summary><code>{0}</code>{1}</summary>", EscapeInline(name), scope));
                sb.AppendLine();

                // Enumerated only when it's a subset. "Failed everywhere" is
                // already said in the summary line, and spelling out all 18
                // platform ids to repeat it is both the least informative
                // case and, at this repo's platform count, most of the
                // document's bytes. A partial failure is the opposite: which
                // platforms is the entire point.
                if (platformCount > 1 && !everywhere)
                {
                    sb.AppendLine(string.Format("**Failed on:** {0}",
                        string.Join(", ", entry.Bad.Select(r => "`" + EscapeInline(r.Platform ?? "") + "`"))));
                    sb.AppendLine();
                }

                // One excerpt, from the first failing platform: the same
                // assertion failing on N platforms produces N near-identical
                // stack traces, and pasting all of them is how this document
                // stops fitting.
                var sample = entry.Bad[0];
                var excerpt = Excerpt(sample.Output);
                if (!string.IsNullOrEmpty(excerpt))
                {
                    var fence = Fence(excerpt);
                    sb.AppendLine(fence);
                    sb.AppendLine(excerpt);
                    sb.AppendLine(fence);
                    sb.AppendLine();
                }

                sb.AppendLine("</details>");
                sb.AppendLine();
            }

            return Math.Max(0, failing.Count - MaxFailureEntries);
        }

        // The per-item caps above bound how many things go in, but not how
        // wide any single one is - one test logging a 900 KB line would
        // still sail past the ceiling. Cut at a line boundary near the
        // budget so the result is still valid markdown, and say so.
        private static string ApplyByteBudget(string text)
        {
            if (Encoding.UTF8.GetByteCount(text) <= ByteBudget)
                return null;

            var lines = text.Split('\n');
            var sb = new StringBuilder();
            var used = 0;
            // Cutting at an arbitrary line can land inside a <details>
            // block, and a dangling unclosed one swallows everything after
            // it - including the truncation notice appended below, i.e.
            // exactly the message explaining what happened. Track where the
            // tags were last balanced and rewind to there, dropping the
            // half-written entry rather than emitting a synthetic close
            // around it.
            var depth = 0;
            var balancedLength = 0;
            foreach (var line in lines)
            {
                var cost = Encoding.UTF8.GetByteCount(line) + 1;
                if (used + cost > ByteBudget)
                    break;
                sb.Append(line).Append('\n');
                used += cost;

                if (line.IndexOf("<details", StringComparison.OrdinalIgnoreCase) >= 0)
                    depth++;
                else if (line.IndexOf("</details>", StringComparison.OrdinalIgnoreCase) >= 0)
                    depth--;

                if (depth == 0)
                    balancedLength = sb.Length;
            }

            if (depth != 0)
                sb.Length = balancedLength;

            sb.AppendLine();
            sb.AppendLine("_Summary truncated to stay under GitHub's 1 MiB job-summary limit. Full detail in the HTML report (`-f html`)._");
            return sb.ToString();
        }

        private static string Excerpt(string output)
        {
            if (string.IsNullOrEmpty(output))
                return "";

            var lines = output.Replace("\r\n", "\n").Split('\n');
            var kept = lines.Take(MaxOutputLines).ToList();
            var truncated = lines.Length > MaxOutputLines;

            var text = string.Join("\n", kept);
            if (text.Length > MaxOutputChars)
            {
                text = text.Substring(0, MaxOutputChars);
                truncated = true;
            }
            if (truncated)
                text += "\n… (truncated)";
            return text;
        }

        // A fence has to be longer than the longest backtick run inside the
        // content, or captured output containing a code fence of its own
        // would end the block early and spill the rest as markup.
        private static string Fence(string content)
        {
            var longest = 0;
            var current = 0;
            foreach (var c in content)
            {
                if (c == '`')
                {
                    current++;
                    if (current > longest)
                        longest = current;
                }
                else
                {
                    current = 0;
                }
            }
            return new string('`', Math.Max(3, longest + 1));
        }

        private static string StripPrefixSafe(FixtureMeta fixture)
        {
            return fixture == null ? "" : ResultsModel.StripPrefix(fixture.UniqueName);
        }

        // A literal pipe would silently split a table cell in two and shift
        // every following column.
        private static string EscapeCell(string value)
        {
            return EscapeInline(value).Replace("|", "\\|");
        }

        // Backticks are what would break out of the inline-code spans these
        // values sit in; the surrounding <summary>/<code> also makes a raw
        // '<' ambiguous.
        private static string EscapeInline(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("`", "&#96;");
        }
    }
}
