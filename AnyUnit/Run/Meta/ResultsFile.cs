using System;
using System.Collections.Generic;
using System.Linq;

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


                return String.Format("{{\"Tool\":\"{0}\", \"SchemaVersion\":{1}, \"Assemblies\":[{2}]}}",
                                     Tool,
                                     SchemaVersion,
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
