using System;
using System.Linq;
using AnyUnit.Run;

namespace SatelliteRunner.Shared
{

public partial class RunTests{

        // Instance, not static: this class is constructed fresh per run (once
        // by RunAloneCommand.Run, once per AnyUnit.Runner.Bootstrap.Runner.Run
        // call) - no reason for one run's TeamCity setting to leak into
        // another's, or to need "don't call Run twice concurrently" caveats.
        // RunAloneCommand.cs's own -teamcity option still has to record the
        // flag on itself first (ManyConsole's HasOption callback fires while
        // parsing options, before RunAloneCommand.Run ever constructs a
        // RunTests to set it on) - only that one, real ordering constraint,
        // not a reason for this field itself to be static.
        //
        // Plain settable field, not a constructor parameter: TeamCity mode is
        // legacy (still real, still might be useful to someone), not core to
        // what this class does - `new RunTests { TeamCity = ... }` keeps that
        // optional without giving it a permanent seat in the constructor
        // signature every future caller has to know about.
        public bool TeamCity;

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