using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit.Run;

namespace CoverageRunner
{
    class Program
    {
        static int Main(string[] args)
        {

            ConventionTestProcessor.CIResultOutput.Disable = true;

            var id = "net40-converage";


            var asms = new[]
                           {
                               Assembly.GetAssembly(typeof (BasicTests.Basic)),
                               Assembly.GetAssembly(typeof (ConstraintsTests.Basic)),
                               Assembly.GetAssembly(typeof (XunitTests.Basic)),
                               Assembly.GetAssembly(typeof (NunitTests.Basic)),
                               Assembly.GetAssembly(typeof (FsUnitTests.BasicTests)),

                           };

            var runner = Runner.Create(id, asms);
            var file = new ResultsFile();
            runner.RunAll(r =>
                              {
                                  string js = r.ToItemJson();
                                  var r2= Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(js);
                                  file.Add(r2);
                              });
            var result =file.ToListJson();

            return ConventionTestProcessor.Program.VerifyJsonResults(new[]{result});
        }
    }
}
