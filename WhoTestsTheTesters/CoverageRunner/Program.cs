using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PclUnit.Runner;

namespace CoverageRunner
{
    class Program
    {
        static int Main(string[] args)
        {

            ConventionTestProcessor.TeamCity.Disable = true;

            var id = "net40-converage";


            var asms = new[]
                           {
                               Assembly.GetAssembly(typeof (BasicTests.Basic)),
                               Assembly.GetAssembly(typeof (ConstraintsTests.Basic)),
                           };

            var runner = Generate.Tests(id, asms);
            var file = new ResultsFile();
            runner.RunAll(r =>
                              {
                                  string js = r.ToItemJson();
                                  var r2= Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(js);
                                  file.Add(r2);
                              });
            var result =file.ToListJson();

            return ConventionTestProcessor.Program.VerifyJsonResults(result);
        }
    }
}
