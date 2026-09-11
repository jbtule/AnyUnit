using System;
using System.Linq;
using AnyUnit.Run;

namespace SatelliteRunner.Shared
{

public partial class RunTests{


        public static bool TeamCity;

        public void PrintOutAloneStart(string id)
        {
            if (TeamCity)
            {
                Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", id);
            }
            else
            {
                Console.WriteLine("Starting Tests for '{0}'", id);
            }
        }

        public void PrintOutAloneEnd(string id, ResultsFile file)
        {
            if (TeamCity)
            {
                Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", id);
            }
            else
            {
                Console.WriteLine("Finished");
                Console.WriteLine();
                var resultCount = file.ResultCount;
                foreach (var kp in resultCount.OrderBy(it=>it.Key))
                {
                    Console.WriteLine("  {0,-15}{1,4}", kp.Key, kp.Value);
                }
                Console.WriteLine("{0,-17}{1,4}", "Total",resultCount.Select(r => r.Value).Sum());
            }
        }

        public void PrintOutAloneResults(Result result)
        {
            if (TeamCity)
            {
                Console.WriteLine("##teamcity[testStarted name='{2}.{1}.{0}' captureStandardOutput='true']",
                                  result.Test.Name, result.Test.Fixture.Name, result.Test.Fixture.Assembly.Name);
            }
            else
            {
                Console.Write(result.Test.Fixture.Assembly.Name + ".");
                Console.Write(result.Test.Fixture.Name + ".");
            }
          
            Console.Write(result.Test.Name);
            Console.WriteLine("[{0}]", result.Platform);
            Console.Write(result.Kind);
            Console.WriteLine(" ({0})", result.EndTime - result.StartTime);
            Console.WriteLine(result.Output);

            if (TeamCity)
            {
                Console.WriteLine("##teamcity[testFinished name='{2}.{1}.{0}' duration='{3}']",
                    result.Test.Name,
                    result.Test.Fixture.Name,
                    result.Test.Fixture.Assembly.Name,
                    (result.EndTime - result.StartTime).TotalMilliseconds);
            }
            else
            {
                switch (result.Kind)
                {
                    case ResultKind.Success:
                        Console.WriteLine("-------------------------");
                        break;
                    case ResultKind.Error:
                        Console.WriteLine("EEEEEEEEEEEEEEEEEEEEEEEEE");
                        break;
                    case ResultKind.Fail:
                        Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!");
                        break;
                    case ResultKind.NoError: 
                        Console.WriteLine(".........................");
                        break;
                    case ResultKind.Ignore:
                        Console.WriteLine("?????????????????????????");
                        break;
                }
              
                Console.WriteLine(String.Empty);
            }
        }
    
}

}