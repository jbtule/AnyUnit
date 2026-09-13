using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AnyUnit.Run;

namespace ConventionTestProcessor
{
    // Surfaces ConventionMatch's Correct/Invalid/Unknown verdict (see that
    // class - the actual "did this self-test behave the way its own name
    // promised" pass/fail signal, not raw Result.Kind) as a GitHub Actions
    // Job Summary, so a PR/run shows something better than "open the raw
    // console log" for what's this repo's real CI gate. A no-op outside
    // GitHub Actions (or when explicitly opted out - see
    // ANYUNIT_SKIP_GH_SUMMARY below), so running this locally (see
    // WhoTestsTheTesters/Readme.md's own documented manual workflow) is
    // unaffected.
    internal static class GitHubSummary
    {
        public static void Write(IList<Result> correct, IList<Result> invalid, IList<Result> unknown, IEnumerable<string> platforms)
        {
            var path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (string.IsNullOrEmpty(path))
                return;

            // Opt-out for a job that runs ConventionTestProcessor for a
            // reason other than the self-test convention gate itself -
            // e.g. build.yml's `coverage` job re-runs the identical
            // self-test data purely to measure code coverage %, so its
            // own copy of this table would just duplicate build-and-
            // test's, not add new information.
            if (Environment.GetEnvironmentVariable("ANYUNIT_SKIP_GH_SUMMARY") == "true")
                return;

            var markdown = Build(correct, invalid, unknown, platforms);

            // append, not overwrite: GITHUB_STEP_SUMMARY is a single file
            // for the whole job, and other steps (or another invocation of
            // this same step) may already have written to it. new
            // UTF8Encoding(false), not Encoding.UTF8 - the latter's
            // default emits a BOM, which would either sit at the very
            // start of the file (harmless there, but pointless) or, worse,
            // appear as stray mid-document garbage if another step
            // already wrote to this file first (see Runner/Bootstrap/
            // Runner.cs's own comment on the same choice).
            using (var writer = new StreamWriter(path, append: true, new UTF8Encoding(false)))
            {
                writer.Write(markdown);
            }
        }

        internal static string Build(IList<Result> correct, IList<Result> invalid, IList<Result> unknown, IEnumerable<string> platforms)
        {
            var sb = new StringBuilder();

            sb.AppendLine("## AnyUnit self-test convention results");
            sb.AppendLine();

            // Which platforms this particular verdict actually covers - one
            // call processing several platforms' results.json at once (see
            // build.yml's convention-summary job) is exactly when this
            // matters: without it, there's no way to tell from the summary
            // alone whether "Correct:374, Invalid:0" means one platform or
            // every one of them.
            var platformList = new List<string>(platforms);
            if (platformList.Count > 0)
            {
                sb.AppendLine(string.Format("**Platforms:** {0}", string.Join(", ", platformList.Select(EscapeCell))));
                sb.AppendLine();
            }

            sb.AppendLine("| Correct | Invalid | Unknown |");
            sb.AppendLine("|---|---|---|");
            sb.AppendLine(string.Format("| {0} | {1} | {2} |", correct.Count, invalid.Count, unknown.Count));
            sb.AppendLine();

            // Invalid first (and left open) - these are real regressions,
            // a self-test's actual outcome no longer matches what its own
            // name promises. Unknown second (and collapsed) - just a test
            // name the naming convention doesn't recognize, not
            // necessarily wrong.
            if (invalid.Count > 0)
                AppendSection(sb, "\u274c Invalid", invalid, open: true);
            if (unknown.Count > 0)
                AppendSection(sb, "\u2753 Unknown", unknown, open: false);

            return sb.ToString();
        }

        private static void AppendSection(StringBuilder sb, string title, IList<Result> results, bool open)
        {
            sb.AppendLine(string.Format("<details{0}>", open ? " open" : ""));
            sb.AppendLine(string.Format("<summary>{0} ({1})</summary>", title, results.Count));
            sb.AppendLine();
            sb.AppendLine("| Test | Platform | Kind | Output |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var result in results)
            {
                sb.AppendLine(string.Format("| {0} | {1} | {2} | {3} |",
                    EscapeCell(FullTestName(result)),
                    EscapeCell(result.Platform),
                    result.Kind,
                    EscapeCell(FirstLine(result.Output))));
            }
            sb.AppendLine();
            sb.AppendLine("</details>");
            sb.AppendLine();
        }

        private static string FullTestName(Result result)
        {
            var test = result.Test;
            var fixture = test.Fixture;
            var assembly = fixture.Assembly;
            return string.Format("{0}.{1}.{2}", assembly.Name, fixture.Name, test.Name);
        }

        // AnyUnit's Result.Output is the test's whole captured log, not a
        // separate short message - show only its first line here (same
        // "first line as summary" convention Report/Formats/JUnitXmlWriter.cs
        // uses for the same reason), not the whole thing, to keep the
        // table readable.
        private static string FirstLine(string output)
        {
            if (string.IsNullOrEmpty(output))
                return string.Empty;
            var newline = output.IndexOfAny(new[] { '\r', '\n' });
            return newline >= 0 ? output.Substring(0, newline) : output;
        }

        // Markdown table cells break on an unescaped "|", and embedding a
        // raw newline (FirstLine above already strips those, but Platform/
        // test names are free-form too) would also break a row.
        private static string EscapeCell(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
