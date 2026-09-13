using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnyUnit.Run;

namespace ConventionTestProcessor
{
    public class Program
    {
        // Doubles as the real test of AnyUnit's JSON output shape: this is
        // the same reader AnyUnit.Report uses (System.Text.Json against the
        // live ResultsFile/AssemblyMeta/FixtureMeta/TestMeta/Result domain
        // types, not separate DTOs). Populate is required, not optional -
        // ToListJson()'s Fixtures/Tests/Results arrays never carry a back-
        // reference to their parent (FixtureMeta.Assembly, TestMeta.Fixture,
        // Result.Test) as JSON; those only get set as a side effect of
        // adding each deserialized item into the parent's existing
        // CallBackList<T> instance (see Utility.CallBackList) - which
        // requires reusing that instance (Populate) rather than the default
        // behavior of constructing a fresh List<T> and assigning it,
        // bypassing the callback entirely. ConventionMatch below reads
        // those back-references (e.g. result.Test.Fixture.Assembly.Name),
        // so without Populate they'd all come back null.
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        };

        static int Main(string[] args)
        {
            var file = args.FirstOrDefault();
            if (!args.Any())
            {
                throw new ArgumentException("Missing argument to process file");
            }


            return VerifyJsonResults(args.Select(File.ReadAllText));
        }

        public static int VerifyJsonResults(IEnumerable<string> jsons)
        {
            // Every distinct Platform actually seen across every input file
            // - most useful when this is called with more than one (build.
            // yml's own convention-summary job passes every platform's own
            // results.json in one call), so the resulting Job Summary can
            // say what the "across every platform" verdict below actually
            // covers.
            var platforms = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var json in jsons)
            {
                var results = JsonSerializer.Deserialize<ResultsFile>(json, Options);

                foreach (var platform in results.Platforms)
                    platforms.Add(platform);

                foreach (var asm in results.Assemblies)
                {
                    using (CIResultOutput.WriteSuite(asm.Name))
                        foreach (var fix in asm.Fixtures)
                        {
                            using (CIResultOutput.WriteSuite(fix.Name))
                                foreach (var test in fix.Tests)
                                {
                                    foreach (var result in test.Results)
                                    {
                                        ConventionMatch.PrintOutResult(result);
                                    }
                                }
                        }
                }
            }


            ConventionMatch.WriteOutTrailer();

            GitHubSummary.Write(ConventionMatch.Correct, ConventionMatch.Invalid, ConventionMatch.Unknown, platforms);

            if (ConventionMatch.Invalid.Count > 0)
            {
                return -1;
            }
            return 0;
        }
    }
}
