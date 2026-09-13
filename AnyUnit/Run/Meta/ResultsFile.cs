using System;
using System.Collections.Generic;
using System.Linq;
using AnyUnit.Util;

namespace AnyUnit.Run
{
    public class ResultsFile : IJsonSerialize
    {
        // Bump when ToListJson()'s shape changes in a way an existing
        // reader (AnyUnit.Report, WhoTestsTheTesters/ConventionTestProcessor,
        // or any other consumer of this JSON) couldn't just ignore - a
        // renamed/removed property, not an additive one. There was no
        // version field at all before this - readers had no way to tell
        // "shape I don't understand" apart from "shape I understand but
        // parsed wrong".
        public const int CurrentSchemaVersion = 1;

        // Self-identifying marker: nothing else about this shape (an
        // object with an "Assemblies" array) is distinctive enough that a
        // tool handed an arbitrary .json file could reliably tell "this is
        // an AnyUnit results file" from "this happens to have a property
        // also called Assemblies" - e.g. before erroring out on an
        // unexpected SchemaVersion, or before a generic file-type sniffer
        // picks a reader for a file with no distinguishing extension.
        public const string ToolName = "AnyUnit";

        public ResultsFile()
        {
            Assemblies = new List<AssemblyMeta>();
            Platforms = new List<string>();
            Tool = ToolName;
            SchemaVersion = CurrentSchemaVersion;
        }

        public string Tool { get; set; }

        public int SchemaVersion { get; set; }

        public IList<AssemblyMeta> Assemblies { get; set; }

        public bool HasError
        {
            get
            {
                return Results
                          .Any(it => it.Kind == ResultKind.Error || it.Kind == ResultKind.Fail);
            }
        }

        public IDictionary<ResultKind, int> ResultCount
        {
            get { return Results.GroupBy(it => it.Kind).ToDictionary(k => k.Key, v => v.Count()); }
        }

        // Every distinct Result.Platform actually present, e.g.
        // ["net10-osx-arm64", "net48-win-x64"] - a top-level summary of
        // what's in this file without having to walk the whole
        // Assemblies/Fixtures/Tests/Results tree first. Most useful after
        // a multi-input merge (see AnyUnit.Report's ConvertCommand) - one
        // glance at a merged file's Platforms tells you which platforms
        // actually got combined into it. Maintained directly in Add()
        // below, not derived from Results on demand - by the time Add()
        // sees a Result, its Platform is already known (that's the one
        // piece of information every caller of Add() - a live run just as
        // much as a multi-file merge - already has in hand), so there's
        // nothing to compute here.
        public IList<string> Platforms { get; set; }

        public IEnumerable<Result> Results
        {
          get
          {
              return Assemblies.SelectMany(it => it.Fixtures)
                        .SelectMany(it => it.Tests)
                        .SelectMany(it => it.Results);
          }
        } 

        public void Add(Result result)
        {

            lock (this)
            {
                if (!Platforms.Contains(result.Platform))
                    Platforms.Add(result.Platform);

                var lv1 = Assemblies.SingleOrDefault(it => it.UniqueName == result.Test.Fixture.Assembly.UniqueName);
                if (lv1 == null)
                {
                    lv1 = result.Test.Fixture.Assembly;
                    Assemblies.Add(lv1);
                }
                var lv2 = lv1.Fixtures.SingleOrDefault(it => it.UniqueName == result.Test.Fixture.UniqueName);
                if (lv2 == null)
                {
                    lv2 = result.Test.Fixture;
                    lv1.Fixtures.Add(lv2);
                }
                var lv3 = lv2.Tests.SingleOrDefault(it => it.UniqueName == result.Test.UniqueName);
                if (lv3 == null)
                {
                    lv3 = result.Test;
                    lv2.Tests.Add(lv3);
                }
                var lv4 = lv3.Results.SingleOrDefault(it => it.Platform == result.Platform);
                if (lv4 == null)
                {
                    lv4 = result;
                    lv3.Results.Add(lv4);
                }
            }
        }



        public string ToListJson()
        {
            lock (this)
            {


                return String.Format("{{\"Tool\":\"{0}\", \"SchemaVersion\":{1}, \"Platforms\":{2}, \"Assemblies\":[{3}]}}",
                                     Tool,
                                     SchemaVersion,
                                     Platforms.ToListJson(),
                                     String.Join(",", Assemblies.Select(it => it.ToListJson()).ToArray())
                    );
            }
        }

        public string ToItemJson()
        {
            return ToListJson();
        }

    }
}
