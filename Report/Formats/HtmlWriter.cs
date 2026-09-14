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
using System.Net;
using System.Text;
using AnyUnit.Run;

namespace AnyUnit.Report.Formats
{
    // A self-contained HTML report - the one output format here that isn't
    // a pre-existing external schema, so it's the only one free to be
    // shaped around what actually makes AnyUnit's data different: the same
    // test run under several platforms at once.
    //
    // Every other writer in this directory has to flatten that away,
    // because JUnit/TRX/NUnit/xUnit/CTRF all assume one result per test -
    // ResultsModel.Flatten fans a multi-platform TestMeta out into N
    // separate "TestName [platform]" test cases, which is the honest
    // mapping into those schemas but scatters a single test's story across
    // N unrelated-looking rows. Here the platform becomes an axis instead:
    // one row per test, one column per platform, so "passes on net10,
    // fails only on net48-win-x86" is a single glance rather than a diff
    // of two reports. Cells with no result at all (a platform that never
    // ran that assembly) are rendered as gaps rather than silently
    // omitted, which is the other thing the flattened formats can't show.
    //
    // Single file by design: inline CSS/JS, no external references at all,
    // so it survives being pulled out of a CI artifact zip and opened over
    // file:// with no network. Rendered server-side (here) rather than as
    // embedded JSON hydrated by script, so the report still reads
    // completely with JavaScript off - the script only adds filtering and
    // collapsing on top (see the no-js class dance below).
    internal class HtmlWriter : IResultsFormatWriter
    {
        public void Write(ResultsFile results, Stream output)
        {
            // Derived from the Results actually present rather than read
            // off ResultsFile.Platforms: this writer builds a fixed column
            // per platform, so a platform listed but carrying no result
            // would render an entire empty column, and (worse) a result
            // whose platform somehow wasn't listed would have nowhere to
            // go. Deriving guarantees columns and cells agree.
            var platforms = results.Results
                .Select(r => r.Platform ?? "")
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToList();

            var columnOf = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < platforms.Count; i++)
                columnOf[platforms[i]] = i;

            using (var w = new StreamWriter(output, new UTF8Encoding(false), 4096, leaveOpen: true))
            {
                WriteHead(w);
                WriteSummary(w, results, platforms);
                WriteControls(w);
                WriteMatrix(w, results, platforms, columnOf);
                WriteFoot(w);
            }
        }

        private static void WriteHead(StreamWriter w)
        {
            w.WriteLine("<!doctype html>");
            w.WriteLine("<html lang=\"en\" class=\"no-js\">");
            w.WriteLine("<head>");
            w.WriteLine("<meta charset=\"utf-8\">");
            w.WriteLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            w.WriteLine("<title>AnyUnit Test Report</title>");
            w.WriteLine("<style>");
            w.WriteLine(Css);
            w.WriteLine("</style>");
            w.WriteLine("</head>");
            w.WriteLine("<body>");
            w.WriteLine("<div class=\"wrap\">");
        }

        private static void WriteSummary(StreamWriter w, ResultsFile results, IList<string> platforms)
        {
            var all = results.Results.ToList();
            var tests = results.Assemblies.SelectMany(a => a.Fixtures).SelectMany(f => f.Tests).Count();

            w.WriteLine("<header class=\"head\">");
            w.WriteLine("<h1>AnyUnit Test Report</h1>");
            w.Write("<p class=\"sub\">");
            w.Write(Html(string.Format(CultureInfo.InvariantCulture,
                "{0} test{1} × {2} platform{3} = {4} result{5}",
                tests, tests == 1 ? "" : "s",
                platforms.Count, platforms.Count == 1 ? "" : "s",
                all.Count, all.Count == 1 ? "" : "s")));
            w.Write(" &middot; generated ");
            w.Write(Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)));
            w.WriteLine("</p>");
            w.WriteLine("</header>");

            // Headline tiles count executions (test x platform), not
            // distinct tests - a test that passes on five platforms and
            // fails on a sixth is genuinely five passes and one failure,
            // and collapsing that to a single verdict per test would hide
            // exactly the thing this report exists to show.
            w.WriteLine("<section class=\"tiles\">");
            Tile(w, "total", "Results", all.Count);
            Tile(w, "pass", "Passed", all.Count(r => r.Kind == ResultKind.Success));
            Tile(w, "fail", "Failed", all.Count(r => r.Kind == ResultKind.Fail));
            Tile(w, "error", "Errored", all.Count(r => r.Kind == ResultKind.Error));
            Tile(w, "skip", "Ignored", all.Count(r => r.Kind == ResultKind.Ignore));
            Tile(w, "other", "No assert", all.Count(r => r.Kind == ResultKind.NoError));
            // Gaps: test x platform combinations with no result at all.
            // Only meaningful because the matrix has a fixed column set -
            // this is the number of blank cells in it, i.e. coverage a
            // flattened report would simply never mention.
            Tile(w, "none", "Not run", Math.Max(0, (tests * platforms.Count) - all.Count));
            w.WriteLine("</section>");

            if (platforms.Count > 0)
            {
                w.WriteLine("<section class=\"pcards\">");
                for (var p = 0; p < platforms.Count; p++)
                {
                    var platform = platforms[p];
                    var forPlatform = all.Where(r => string.Equals(r.Platform ?? "", platform, StringComparison.Ordinal)).ToList();
                    var sample = forPlatform.FirstOrDefault();
                    var bad = forPlatform.Count(r => r.Kind == ResultKind.Fail || r.Kind == ResultKind.Error);

                    // data-col ties the card to the matrix column it shows
                    // or hides. The card carries no role/tabindex/aria here
                    // - the script adds those (see Js), because without it
                    // the card cannot filter anything and must not claim to.
                    w.Write("<div class=\"pcard");
                    w.Write(bad > 0 ? " has-bad" : "");
                    w.Write("\" data-col=\"");
                    w.Write(p.ToString(CultureInfo.InvariantCulture));
                    w.WriteLine("\">");
                    w.Write("<div class=\"pname\">");
                    w.Write(Html(platform));
                    w.WriteLine("</div>");
                    if (sample != null)
                    {
                        w.Write("<div class=\"pmeta\">");
                        w.Write(Html(Coalesce(sample.FrameworkDescription, "unknown runtime")));
                        w.WriteLine("</div>");
                        w.Write("<div class=\"pmeta\">");
                        w.Write(Html(Coalesce(sample.OSDescription, "unknown OS")));
                        if (!string.IsNullOrEmpty(sample.OSArchitecture))
                        {
                            w.Write(" &middot; ");
                            w.Write(Html(sample.OSArchitecture));
                        }
                        w.WriteLine("</div>");
                    }
                    w.Write("<div class=\"pcounts\">");
                    StatChips(w, forPlatform, Math.Max(0, tests - forPlatform.Count));
                    w.WriteLine("</div>");
                    w.WriteLine("</div>");
                }
                w.WriteLine("</section>");
            }
        }

        // `kind` doubles as the CSS modifier and, for everything except the
        // "total" tile, the filter key matching a row's data-kinds. The
        // total tile gets no data-kind and so stays inert - "filter to all
        // results" is what having nothing selected already means.
        //
        // No role/tabindex/aria-pressed is written here on purpose: see the
        // Js constant. A tile that looks pressable in a viewer that can't
        // run the script would be lying about what it does.
        private static void Tile(StreamWriter w, string kind, string label, int count)
        {
            w.Write("<div class=\"tile k-");
            w.Write(kind);
            w.Write("\"");
            if (!string.Equals(kind, "total", StringComparison.Ordinal))
            {
                w.Write(" data-kind=\"");
                w.Write(kind);
                w.Write("\"");
            }
            w.Write("><div class=\"n\">");
            w.Write(count.ToString(CultureInfo.InvariantCulture));
            w.Write("</div><div class=\"l\">");
            w.Write(Html(label));
            w.WriteLine("</div></div>");
        }

        // The platform checkbox row that used to live here is gone: the
        // platform cards above are the same information in a far bigger tap
        // target, so selecting a platform is now done by pressing its card
        // rather than by hitting a 13px checkbox beside a repeated copy of
        // its name. "Failures only" went with it - the Failed and Errored
        // tiles say the same thing more precisely, and keeping both would
        // have meant two controls competing over one filter.
        private static void WriteControls(StreamWriter w)
        {
            w.WriteLine("<section class=\"controls\">");
            w.WriteLine("<input id=\"q\" type=\"search\" placeholder=\"Filter tests…\" autocomplete=\"off\">");
            w.WriteLine("<button id=\"clearFilters\" type=\"button\" hidden>Clear filters</button>");
            w.WriteLine("<button id=\"expandAll\" type=\"button\">Expand all</button>");
            w.WriteLine("<button id=\"collapseAll\" type=\"button\">Collapse all</button>");
            w.WriteLine("</section>");

            w.WriteLine("<p class=\"legend\">");
            w.WriteLine("<span class=\"g k-pass\">✓</span> passed");
            w.WriteLine("<span class=\"g k-fail\">✕</span> failed");
            w.WriteLine("<span class=\"g k-error\">!</span> errored");
            w.WriteLine("<span class=\"g k-skip\">⊘</span> ignored");
            w.WriteLine("<span class=\"g k-other\">∅</span> no assert");
            w.WriteLine("<span class=\"g k-none\">·</span> not run");
            w.WriteLine("</p>");
        }

        private static void WriteMatrix(StreamWriter w, ResultsFile results, IList<string> platforms,
                                        IDictionary<string, int> columnOf)
        {
            var detailId = 0;

            if (!results.Assemblies.Any())
            {
                w.WriteLine("<p class=\"empty\">No results.</p>");
                return;
            }

            foreach (var assembly in results.Assemblies)
            {
                var assemblyResults = assembly.Fixtures.SelectMany(f => f.Tests).SelectMany(t => t.Results).ToList();

                var assemblyTests = assembly.Fixtures.SelectMany(f => f.Tests).Count();

                w.WriteLine("<section class=\"asm\">");
                w.Write("<h2>");
                w.Write(Html(ResultsModel.StripPrefix(assembly.UniqueName)));
                w.Write(" ");
                StatChips(w, assemblyResults,
                          Math.Max(0, (assemblyTests * platforms.Count) - assemblyResults.Count));
                w.WriteLine("</h2>");

                foreach (var fixture in assembly.Fixtures)
                {
                    var fixtureResults = fixture.Tests.SelectMany(t => t.Results).ToList();
                    var fixtureBad = fixtureResults.Count(r => r.Kind == ResultKind.Fail || r.Kind == ResultKind.Error);

                    w.Write("<details class=\"fix\"");
                    // Open by default only where something needs looking
                    // at - a green fixture starts collapsed so a big
                    // multi-platform run opens on its failures rather than
                    // on several thousand passing cells.
                    w.Write(fixtureBad > 0 ? " open" : "");
                    w.WriteLine(">");
                    w.Write("<summary><span class=\"fname\">");
                    w.Write(Html(ResultsModel.StripPrefix(fixture.UniqueName)));
                    w.Write("</span> ");
                    StatChips(w, fixtureResults,
                              Math.Max(0, (fixture.Tests.Count * platforms.Count) - fixtureResults.Count));
                    w.WriteLine("</summary>");

                    w.WriteLine("<div class=\"scroll\">");
                    w.WriteLine("<table>");

                    w.WriteLine("<thead><tr><th class=\"corner\">Test</th>");
                    for (var i = 0; i < platforms.Count; i++)
                    {
                        w.Write("<th class=\"ph c");
                        w.Write(i.ToString(CultureInfo.InvariantCulture));
                        w.Write("\"><span>");
                        w.Write(Html(platforms[i]));
                        w.WriteLine("</span></th>");
                    }
                    w.WriteLine("</tr></thead>");

                    w.WriteLine("<tbody>");
                    foreach (var test in fixture.Tests)
                    {
                        var byPlatform = new Dictionary<string, Result>(StringComparer.Ordinal);
                        foreach (var result in test.Results)
                            byPlatform[result.Platform ?? ""] = result;

                        var anyBad = test.Results.Any(r => r.Kind == ResultKind.Fail || r.Kind == ResultKind.Error);
                        var hasDetail = test.Results.Any(HasDetail);
                        var id = hasDetail ? "d" + (++detailId).ToString(CultureInfo.InvariantCulture) : null;

                        // Which kinds this row contains at all, "none"
                        // included, so the headline tiles can filter to it
                        // without the script having to re-read every cell.
                        // Padded with spaces at both ends so a substring
                        // test for " fail " can't also match " none ".
                        var kinds = new List<string>();
                        for (var i = 0; i < platforms.Count; i++)
                        {
                            Result at;
                            var kindHere = byPlatform.TryGetValue(platforms[i], out at) ? KindClass(at.Kind) : "none";
                            if (!kinds.Contains(kindHere))
                                kinds.Add(kindHere);
                        }

                        w.Write("<tr class=\"row");
                        w.Write(hasDetail ? " hasdetail" : "");
                        w.Write("\" data-kinds=\" ");
                        w.Write(string.Join(" ", kinds.ToArray()));
                        w.Write(" \" data-fail=\"");
                        w.Write(anyBad ? "1" : "0");
                        w.Write("\" data-search=\"");
                        // Fixture name included so a search for a fixture
                        // matches its tests rather than nothing.
                        w.Write(Html((ResultsModel.StripPrefix(fixture.UniqueName) + " " + test.Name).ToLowerInvariant()));
                        w.Write("\"");
                        if (id != null)
                        {
                            w.Write(" data-detail=\"");
                            w.Write(id);
                            w.Write("\"");
                        }
                        w.WriteLine(">");

                        w.Write("<th scope=\"row\" class=\"tname\"><span>");
                        w.Write(Html(test.Name));
                        w.WriteLine("</span></th>");

                        for (var i = 0; i < platforms.Count; i++)
                        {
                            Result result;
                            var present = byPlatform.TryGetValue(platforms[i], out result);
                            var kind = present ? KindClass(result.Kind) : "none";

                            w.Write("<td class=\"cell c");
                            w.Write(i.ToString(CultureInfo.InvariantCulture));
                            w.Write(" k-");
                            w.Write(kind);
                            w.Write("\" data-platform=\"");
                            w.Write(Html(platforms[i]));
                            w.Write("\" title=\"");
                            w.Write(Html(present
                                ? string.Format(CultureInfo.InvariantCulture, "{0} — {1} ({2})",
                                    test.Name, result.Kind, platforms[i])
                                : string.Format(CultureInfo.InvariantCulture, "{0} — not run on {1}",
                                    test.Name, platforms[i])));
                            w.Write("\">");
                            w.Write(Glyph(kind));
                            w.WriteLine("</td>");
                        }
                        w.WriteLine("</tr>");

                        if (id != null)
                        {
                            w.Write("<tr class=\"detail\" id=\"");
                            w.Write(id);
                            w.Write("\" hidden><td colspan=\"");
                            w.Write((platforms.Count + 1).ToString(CultureInfo.InvariantCulture));
                            w.WriteLine("\">");
                            foreach (var result in test.Results.Where(HasDetail)
                                                      .OrderBy(r => r.Platform ?? "", StringComparer.Ordinal))
                            {
                                w.Write("<div class=\"dblock\" data-platform=\"");
                                w.Write(Html(result.Platform ?? ""));
                                w.WriteLine("\">");
                                w.Write("<div class=\"dhead\"><span class=\"badge k-");
                                w.Write(KindClass(result.Kind));
                                w.Write("\">");
                                w.Write(Html(result.Kind.ToString()));
                                w.Write("</span> <code>");
                                w.Write(Html(result.Platform ?? ""));
                                w.Write("</code> <span class=\"muted\">");
                                w.Write(Html(FormatDuration(result.EndTime - result.StartTime)));
                                if (result.AssertCount >= 0)
                                {
                                    w.Write(string.Format(CultureInfo.InvariantCulture, " · {0} assert{1}",
                                        result.AssertCount, result.AssertCount == 1 ? "" : "s"));
                                }
                                w.WriteLine("</span></div>");
                                // Message, stack trace and log as three
                                // distinct blocks rather than one blob -
                                // they were only ever one blob because
                                // Output was the only field that existed.
                                // Each is skipped when absent, so a
                                // results.json written before those fields
                                // existed renders exactly as it used to:
                                // one <pre> holding the log.
                                WritePre(w, result.SkipReason, "Reason");
                                WritePre(w, result.Message, "Message");
                                WritePre(w, result.StackTrace, "Stack trace");

                                var hasStructured = !string.IsNullOrEmpty(result.SkipReason)
                                                || !string.IsNullOrEmpty(result.Message)
                                                || !string.IsNullOrEmpty(result.StackTrace);
                                if (!string.IsNullOrEmpty(result.Output))
                                    WritePre(w, result.Output, hasStructured ? "Output" : null);
                                else if (!hasStructured)
                                    WritePre(w, "(no output)", null);

                                w.WriteLine("</div>");
                            }
                            w.WriteLine("</td></tr>");
                        }
                    }
                    w.WriteLine("</tbody>");
                    w.WriteLine("</table>");
                    w.WriteLine("</div>");
                    w.WriteLine("</details>");
                }
                w.WriteLine("</section>");
            }
        }

        private static void WriteFoot(StreamWriter w)
        {
            w.WriteLine("</div>");
            w.WriteLine("<script>");
            w.WriteLine(Js);
            w.WriteLine("</script>");
            w.WriteLine("</body>");
            w.WriteLine("</html>");
        }

        // One <pre>, optionally preceded by a small label. The label is
        // omitted when there's only one block to show, so a plain
        // log-only result looks exactly as it did before there was
        // anything to distinguish it from.
        private static void WritePre(StreamWriter w, string text, string label)
        {
            if (string.IsNullOrEmpty(text))
                return;
            if (label != null)
            {
                w.Write("<div class=\"muted\">");
                w.Write(Html(label));
                w.WriteLine("</div>");
            }
            w.Write("<pre>");
            w.Write(Html(text));
            w.WriteLine("</pre>");
        }

        // A result earns a detail block if it either carries output or went
        // wrong - a Fail/Error with an empty Output still needs a row to
        // say so, rather than looking like a test with nothing to report.
        private static bool HasDetail(Result result)
        {
            return !string.IsNullOrEmpty(result.Output)
                || !string.IsNullOrEmpty(result.Message)
                || !string.IsNullOrEmpty(result.SkipReason)
                || result.Kind == ResultKind.Fail
                || result.Kind == ResultKind.Error;
        }

        // Every non-zero kind, not just passed-and-failing. The old summary
        // ("12 passed" / "12 passed, 3 failing") silently folded ignored,
        // no-assert and never-ran into nothing at all, so a callout reading
        // "12 passed" could be hiding three ignored tests and a platform
        // that never ran the assembly - which is the exact question this
        // report is opened to answer. Zero counts stay omitted, so a
        // wholly-green scope still reads as one short chip, not six.
        //
        // `notRun` is passed in rather than derived: it's a property of the
        // matrix (tests x platforms minus results present), which this
        // function can't see from a flat result list.
        private static void StatChips(StreamWriter w, IList<Result> results, int notRun)
        {
            w.Write("<span class=\"stats\">");
            Chip(w, "pass", results.Count(r => r.Kind == ResultKind.Success));
            Chip(w, "fail", results.Count(r => r.Kind == ResultKind.Fail));
            Chip(w, "error", results.Count(r => r.Kind == ResultKind.Error));
            Chip(w, "skip", results.Count(r => r.Kind == ResultKind.Ignore));
            Chip(w, "other", results.Count(r => r.Kind == ResultKind.NoError));
            Chip(w, "none", notRun);
            w.Write("</span>");
        }

        private static void Chip(StreamWriter w, string kindClass, int count)
        {
            if (count <= 0)
                return;
            w.Write("<span class=\"chip k-");
            w.Write(kindClass);
            w.Write("\" title=\"");
            w.Write(Html(string.Format(CultureInfo.InvariantCulture, "{0} {1}", count, KindLabel(kindClass))));
            w.Write("\"><span class=\"g\">");
            w.Write(Glyph(kindClass));
            w.Write("</span>");
            w.Write(count.ToString(CultureInfo.InvariantCulture));
            w.Write("</span>");
        }

        // Single source for the wording, so the tiles, the chips' tooltips
        // and the legend can't drift apart.
        private static string KindLabel(string kindClass)
        {
            switch (kindClass)
            {
                case "pass": return "passed";
                case "fail": return "failed";
                case "error": return "errored";
                case "skip": return "ignored";
                case "other": return "no assert";
                default: return "not run";
            }
        }

        private static string KindClass(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Success: return "pass";
                case ResultKind.Fail: return "fail";
                case ResultKind.Error: return "error";
                case ResultKind.Ignore: return "skip";
                default: return "other";
            }
        }

        // Glyph as well as colour, deliberately: the matrix is read at a
        // glance and colour alone would leave it unreadable to anyone with
        // a red/green colour vision deficiency.
        private static string Glyph(string kindClass)
        {
            switch (kindClass)
            {
                case "pass": return "✓";
                case "fail": return "✕";
                case "error": return "!";
                case "skip": return "⊘";
                case "other": return "∅";
                default: return "·";
            }
        }

        private static string FormatDuration(TimeSpan span)
        {
            if (span < TimeSpan.Zero)
                span = TimeSpan.Zero;
            return span.TotalSeconds >= 1
                ? span.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture) + "s"
                : span.TotalMilliseconds.ToString("0", CultureInfo.InvariantCulture) + "ms";
        }

        private static string Coalesce(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        // Everything user-controlled (test names, platform ids, captured
        // output, stack traces) goes through here on its way into the
        // document - output in particular is arbitrary text from a test
        // run and routinely contains angle brackets and quotes.
        private static string Html(string value)
        {
            return WebUtility.HtmlEncode(value ?? "");
        }

        private const string Css = @"
:root{
  --bg:#ffffff; --surface:#f7f8fa; --surface2:#eceff4; --border:#e2e6ec;
  --text:#1b1f27; --muted:#646d7e; --accent:#2563eb;
  --pass:#177245; --fail:#c0392b; --error:#b3541e; --skip:#8a6d1f; --other:#4a5568; --none:#b9c0cc;
}
@media (prefers-color-scheme: dark){
  :root{
    --bg:#0f1115; --surface:#161a21; --surface2:#1e232c; --border:#2a3039;
    --text:#e6e9ef; --muted:#98a1b3; --accent:#6ea8fe;
    --pass:#45c07a; --fail:#f4705f; --error:#f0a04a; --skip:#d8bd63; --other:#93a0b4; --none:#4a5260;
  }
}
*{box-sizing:border-box}
html,body{margin:0;padding:0}
body{
  background:var(--bg); color:var(--text);
  font:14px/1.5 -apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;
  -webkit-text-size-adjust:100%;
}
[hidden]{display:none!important}
.wrap{max-width:1400px;margin:0 auto;padding:16px}
@media (max-width:600px){ .wrap{padding:12px} }

.head h1{margin:0 0 2px;font-size:20px;letter-spacing:-.01em}
.sub{margin:0 0 16px;color:var(--muted);font-size:13px}

.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(92px,1fr));gap:8px;margin-bottom:14px}
.tile{background:var(--surface);border:1px solid var(--border);border-radius:10px;padding:10px 12px}
.tile .n{font-size:20px;font-weight:650;line-height:1.2}
.tile .l{font-size:11px;color:var(--muted);text-transform:uppercase;letter-spacing:.04em}
.tile.k-pass .n{color:var(--pass)} .tile.k-fail .n{color:var(--fail)}
.tile.k-error .n{color:var(--error)} .tile.k-skip .n{color:var(--skip)}
.tile.k-other .n{color:var(--other)} .tile.k-none .n{color:var(--none)}

.pcards{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:8px;margin-bottom:14px}
.pcard{background:var(--surface);border:1px solid var(--border);border-left:3px solid var(--pass);border-radius:8px;padding:8px 10px}
.pcard.has-bad{border-left-color:var(--fail)}
.pname{font-weight:600;font-size:13px;word-break:break-all}
.pmeta{color:var(--muted);font-size:11px;margin-top:1px;word-break:break-word}
.pcounts{font-size:11px;margin-top:6px;color:var(--muted)}

/* Stat chips: one per non-zero kind, on every assembly, fixture and
   platform callout. Glyph + number, never colour alone - same reasoning
   as the matrix cells. */
.stats{display:inline-flex;flex-wrap:wrap;gap:3px 6px;vertical-align:middle;font-weight:400}
.chip{display:inline-flex;align-items:center;gap:3px;font-size:11px;font-variant-numeric:tabular-nums;
      padding:1px 6px;border-radius:999px;background:var(--surface);border:1px solid var(--border);color:var(--muted)}
.chip .g{font-weight:700;line-height:1}
.chip.k-pass .g{color:var(--pass)} .chip.k-fail .g{color:var(--fail)}
.chip.k-error .g{color:var(--error)} .chip.k-skip .g{color:var(--skip)}
.chip.k-other .g{color:var(--other)} .chip.k-none .g{color:var(--none)}
.chip.k-fail,.chip.k-error{color:var(--text);font-weight:600}

/* Interactive affordance is applied ONLY to elements the script has
   actually wired up (it adds .sel-able itself). With JavaScript off
   nothing below matches, so the tiles and platform cards stay plain
   readouts rather than pretending to be buttons that never respond. */
.sel-able{cursor:pointer;user-select:none;transition:border-color .12s,box-shadow .12s,opacity .12s}
.sel-able:hover{border-color:var(--muted)}
.sel-able:focus-visible{outline:2px solid var(--accent);outline-offset:2px}
.tiles.has-sel .tile.sel-able:not(.on){opacity:.45}
.tile.sel-able.on{border-color:var(--accent);box-shadow:inset 0 0 0 1px var(--accent)}
.pcards.has-sel .pcard.sel-able:not(.on){opacity:.45}
.pcard.sel-able.on{box-shadow:inset 0 0 0 1px var(--accent)}
.sel-able .selmark{float:right;font-size:10px;color:var(--accent);font-weight:700}

.controls{display:flex;flex-wrap:wrap;gap:8px;align-items:center;margin-bottom:8px}
.controls input[type=search]{
  flex:1 1 200px;min-width:0;padding:7px 10px;border:1px solid var(--border);
  border-radius:8px;background:var(--bg);color:var(--text);font-size:14px;
}
.controls button{
  padding:7px 11px;border:1px solid var(--border);border-radius:8px;
  background:var(--surface);color:var(--text);font-size:13px;cursor:pointer;
}
.controls button:hover{border-color:var(--accent);color:var(--accent)}

.legend{display:flex;flex-wrap:wrap;gap:4px 14px;align-items:center;color:var(--muted);font-size:12px;margin:0 0 14px}
.legend .g{display:inline-block;width:18px;text-align:center;font-weight:700;margin-right:2px}

.asm{margin-bottom:18px}
.asm h2{font-size:15px;margin:0 0 6px;letter-spacing:-.01em;word-break:break-word}

.fix{background:var(--surface);border:1px solid var(--border);border-radius:8px;margin-bottom:6px;overflow:hidden}
.fix>summary{padding:8px 10px;cursor:pointer;font-size:13px;display:flex;flex-wrap:wrap;gap:4px 8px;align-items:baseline}
.fix>summary:hover{background:var(--surface2)}
.fname{font-weight:600;word-break:break-word}

.scroll{overflow-x:auto;border-top:1px solid var(--border);-webkit-overflow-scrolling:touch}
table{border-collapse:separate;border-spacing:0;width:auto;min-width:100%;font-size:13px}
thead th{position:sticky;top:0;z-index:2;background:var(--surface2)}
th,td{border-bottom:1px solid var(--border)}

/* Vertical column headers: a dozen platform ids across the top is the
   normal case here, and 'net10-browser-wasm' horizontally would force a
   ~200px column for a cell holding one glyph. */
th.ph{
  padding:6px 2px;vertical-align:bottom;font-weight:600;font-size:11px;
  width:30px;min-width:30px;max-width:30px;
}
th.ph span{writing-mode:vertical-rl;transform:rotate(180deg);white-space:nowrap;color:var(--muted)}
th.corner{
  position:sticky;left:0;z-index:3;background:var(--surface2);
  text-align:left;padding:6px 10px;font-size:11px;color:var(--muted);
  text-transform:uppercase;letter-spacing:.04em;
}
th.tname{
  position:sticky;left:0;z-index:1;background:var(--surface);
  text-align:left;font-weight:400;padding:4px 10px;max-width:340px;
}
th.tname span{display:block;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
@media (max-width:600px){ th.tname{max-width:170px} }

.row:hover th.tname,.row:hover td{background:var(--surface2)}
.row.hasdetail{cursor:pointer}
.row.open th.tname,.row.open td{background:var(--surface2)}

td.cell{text-align:center;padding:0;font-weight:700;line-height:26px;height:26px}
td.k-pass{color:var(--pass)} td.k-fail{color:var(--fail)} td.k-error{color:var(--error)}
td.k-skip{color:var(--skip)} td.k-other{color:var(--other)} td.k-none{color:var(--none)}

tr.detail>td{padding:6px 10px;background:var(--bg)}
.dblock{border-left:3px solid var(--border);padding:2px 0 2px 8px;margin:6px 0}
.dblock.hl{border-left-color:var(--accent)}
.dhead{display:flex;flex-wrap:wrap;gap:6px;align-items:center;font-size:12px;margin-bottom:3px}
.dhead code{font-size:11px;color:var(--muted);word-break:break-all}
.badge{
  display:inline-block;padding:0 7px;border-radius:99px;font-size:11px;
  font-weight:650;border:1px solid currentColor;
}
.badge.k-pass{color:var(--pass)} .badge.k-fail{color:var(--fail)} .badge.k-error{color:var(--error)}
.badge.k-skip{color:var(--skip)} .badge.k-other{color:var(--other)}
.muted{color:var(--muted)}
pre{
  margin:0;padding:8px 10px;background:var(--surface2);border-radius:6px;
  overflow-x:auto;font-size:12px;line-height:1.45;
  font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;
  white-space:pre-wrap;word-break:break-word;
}
.empty{color:var(--muted);padding:24px 0;text-align:center}

/* Without script there is nothing to open a detail row with, so show
   them all rather than hiding output behind an interaction that cannot
   happen. */
.no-js tr.detail{display:table-row!important}
.no-js .controls{display:none}
";

        private const string Js = @"
(function(){
  var doc = document;
  doc.documentElement.classList.remove('no-js');

  var q = doc.getElementById('q');
  var clearBtn = doc.getElementById('clearFilters');
  var colStyle = doc.createElement('style');
  doc.head.appendChild(colStyle);

  var tiles = doc.querySelectorAll('.tile[data-kind]');
  var cards = doc.querySelectorAll('.pcard[data-col]');
  var tileWrap = doc.querySelector('.tiles');
  var cardWrap = doc.querySelector('.pcards');

  // Both selections are ADDITIVE and start empty, meaning ''no filter'' -
  // not ''everything deselected''. That is what lets one tap do something
  // useful (show just the failures) instead of needing five taps to turn
  // the other kinds off first, which is how the checkbox row it replaces
  // behaved.
  var kinds = {};   // kind -> true, empty = all kinds
  var cols = {};    // column index -> true, empty = all columns

  function any(map){ for (var k in map) if (map[k]) return true; return false; }

  // Columns are hidden with a generated stylesheet rather than by touching
  // every cell: one rule per hidden platform beats walking thousands of
  // <td>s on each toggle.
  function syncColumns(){
    var rules = [];
    if (any(cols)){
      for (var i = 0; i < cards.length; i++){
        var c = cards[i].getAttribute('data-col');
        if (!cols[c]) rules.push('.c' + c + '{display:none}');
      }
    }
    colStyle.textContent = rules.join('');
  }

  function applyFilters(){
    var term = (q && q.value ? q.value : '').toLowerCase().trim();
    var byKind = any(kinds);
    var fixtures = doc.querySelectorAll('details.fix');

    for (var f = 0; f < fixtures.length; f++){
      var rows = fixtures[f].querySelectorAll('tr.row');
      var visible = 0;
      for (var i = 0; i < rows.length; i++){
        var row = rows[i];
        var show = true;
        if (term && row.getAttribute('data-search').indexOf(term) < 0) show = false;
        if (show && byKind){
          // data-kinds is space-padded at both ends, so ' fail ' cannot
          // also match ' none '.
          var have = row.getAttribute('data-kinds') || '';
          var hit = false;
          for (var k in kinds){ if (kinds[k] && have.indexOf(' ' + k + ' ') >= 0){ hit = true; break; } }
          if (!hit) show = false;
        }
        row.hidden = !show;
        if (show) visible++;
        var id = row.getAttribute('data-detail');
        if (id && !show){
          var detail = doc.getElementById(id);
          if (detail) detail.hidden = true;
          row.classList.remove('open');
        }
      }
      // A fixture whose every test is filtered out is noise, not context.
      fixtures[f].hidden = (visible === 0);
      if (visible > 0 && (term || byKind)) fixtures[f].open = true;
    }

    var active = !!term || byKind || any(cols);
    if (clearBtn) clearBtn.hidden = !active;
    if (tileWrap) tileWrap.classList.toggle('has-sel', byKind);
    if (cardWrap) cardWrap.classList.toggle('has-sel', any(cols));
  }

  function paint(el, on){
    el.classList.toggle('on', on);
    el.setAttribute('aria-pressed', on ? 'true' : 'false');
    var mark = el.querySelector('.selmark');
    if (mark) mark.textContent = on ? '\u25CF' : '';
  }

  // role/tabindex/aria-pressed are added HERE, never in the generated
  // markup: without this script running, these elements cannot filter
  // anything, and an element that announces itself as a pressed-state
  // button while doing nothing is worse than a plain readout.
  function wire(el, toggle){
    el.classList.add('sel-able');
    el.setAttribute('role', 'button');
    el.setAttribute('tabindex', '0');
    el.setAttribute('aria-pressed', 'false');
    var mark = doc.createElement('span');
    mark.className = 'selmark';
    el.insertBefore(mark, el.firstChild);
    el.addEventListener('click', toggle);
    el.addEventListener('keydown', function(e){
      if (e.key === 'Enter' || e.key === ' ' || e.key === 'Spacebar'){ e.preventDefault(); toggle(); }
    });
  }

  for (var t = 0; t < tiles.length; t++){
    (function(el){
      var kind = el.getAttribute('data-kind');
      wire(el, function(){
        kinds[kind] = !kinds[kind];
        paint(el, !!kinds[kind]);
        applyFilters();
      });
    })(tiles[t]);
  }

  for (var c = 0; c < cards.length; c++){
    (function(el){
      var col = el.getAttribute('data-col');
      wire(el, function(){
        cols[col] = !cols[col];
        paint(el, !!cols[col]);
        syncColumns();
        applyFilters();
      });
    })(cards[c]);
  }

  if (clearBtn) clearBtn.addEventListener('click', function(){
    kinds = {}; cols = {};
    if (q) q.value = '';
    for (var i = 0; i < tiles.length; i++) paint(tiles[i], false);
    for (var j = 0; j < cards.length; j++) paint(cards[j], false);
    syncColumns();
    applyFilters();
  });

  function toggleRow(row, platform){
    var id = row.getAttribute('data-detail');
    if (!id) return;
    var detail = doc.getElementById(id);
    if (!detail) return;
    var open = detail.hidden;
    detail.hidden = !open;
    row.classList.toggle('open', open);

    var blocks = detail.querySelectorAll('.dblock');
    for (var i = 0; i < blocks.length; i++){
      blocks[i].classList.toggle('hl',
        open && !!platform && blocks[i].getAttribute('data-platform') === platform);
    }
  }

  doc.addEventListener('click', function(e){
    var cell = e.target.closest ? e.target.closest('td.cell, th.tname') : null;
    if (!cell) return;
    var row = cell.closest('tr.row');
    if (!row) return;
    toggleRow(row, cell.getAttribute('data-platform'));
  });

  if (q) q.addEventListener('input', applyFilters);

  function setAll(open){
    var fixtures = doc.querySelectorAll('details.fix');
    for (var i = 0; i < fixtures.length; i++) fixtures[i].open = open;
  }
  var expand = doc.getElementById('expandAll');
  var collapse = doc.getElementById('collapseAll');
  if (expand) expand.addEventListener('click', function(){ setAll(true); });
  if (collapse) collapse.addEventListener('click', function(){ setAll(false); });
})();
";
    }
}
