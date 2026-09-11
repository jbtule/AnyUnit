using System;
using System.Collections.Generic;
using System.Linq;

namespace AnyUnit.Run
{
    public class ResultsFile : IJsonSerialize
    {

        public ResultsFile()
        {
            Assemblies = new List<AssemblyMeta>();
        }

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


                return String.Format("{{Assemblies:[{0}]}}",
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
