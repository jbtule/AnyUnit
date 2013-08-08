using System;

namespace ConventionTestProcessor
{
    public static class TeamCity
    {

        private static bool _teamCityRunner;

        static TeamCity()
        {
            _teamCityRunner = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
        }

        public class EndSuite:IDisposable
        {
            private readonly string _name;

            public EndSuite(string name)
            {
                _name = name;
            }


            public void Dispose()
            {
                TeamCity.WriteLine("##teamcity[testSuiteFinished name='{0}']", Encode(_name));
            }
        }

        public static bool Disable { get; set; }

        public static bool ShouldDisplay
        {
            get { return _teamCityRunner && !Disable; }
        }

        public static IDisposable WriteSuite(string name)
        {
            WriteLine("##teamcity[testSuiteStarted name='{0}']", Encode(name));
            return  new EndSuite(name);
        }

        public static string Encode(string value)
        {
            value = value.Replace("|", "||");
            value = value.Replace("'", "|'");
            value = value.Replace("\n", "|n");
            value = value.Replace("\r", "|r");
            value = value.Replace("[", "|[");
            value = value.Replace("]", "|]");
            return value;
        }


        public static void WriteLine(string format, params object[] objs)
        {
            if (ShouldDisplay)
                Console.WriteLine(format, objs);
        }
        public static void DontWriteLine(string format, params object[] objs)
        {
            if (!ShouldDisplay)
                Console.WriteLine(format, objs);
        }
        public static void DontWrite(string format, params object[] objs)
        {
            if (!ShouldDisplay)
                Console.Write(format, objs);
        }

    }
}