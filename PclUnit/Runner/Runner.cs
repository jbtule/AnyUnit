using System;
using System.Collections.Generic;
using System.Linq;

namespace PclUnit.Runner
{
    public class Runner:IJsonSerialize
    {
        public Runner()
        {
            Assemblies = new List<AssemblyMeta>();
            Tests = new List<Test>();
        }

        public Runner(string platform):this()
        {
            Platform = platform;

        }

        public string Platform { get; set; }
        public IList<AssemblyMeta> Assemblies { get; set; }
        public IList<Test> Tests { get; set; }

        public void RunAll(Action<Result> resultCallBack, TestFilter testFilter = null)
        {
            testFilter = testFilter ?? new TestFilter();


            foreach (var test in Tests)
            {
                if(!testFilter.ShouldRun(test))
                    continue;
                var result = test.Run(Platform);
                resultCallBack(result);
            }
        }
        
      
        
        public string ToListJson()
        {
            return String.Format("{{Platform:\"{1}\", Assemblies:[{0}]}}",
                          String.Join(",", Assemblies.Select(it => it.ToListJson()).ToArray()), Platform);
        }

        public string ToItemJson()
        {
            return ToListJson();
        }

 
    }
}