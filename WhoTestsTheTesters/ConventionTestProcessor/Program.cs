using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using AnyUnit.Run;

namespace ConventionTestProcessor
{
    public class Program
    {


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

            foreach (var json in jsons)
            {
                var results = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultsFile>(json);

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


            if (ConventionMatch.Invalid.Count > 0)
            {
                return -1;
            }
            return 0;
        }
    }
}
