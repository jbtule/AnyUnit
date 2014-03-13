using System;
using System.IO;
using System.Net;
using System.Text;
using PclUnit.Run;
using PclUnit.Util;

namespace ConventionTestProcessor
{
    public static class CIResultOutput
    {

        private static bool _teamCityRunner;
        private static bool _appVeyorRunner;


        static CIResultOutput()
        {
            _teamCityRunner = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEAMCITY_VERSION"));
            _appVeyorRunner = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("APPVEYOR_API_URL"));
        }

        private static Uri _appVeyorBaseUri = null;
        private static void PostToAppVeyor(string json)
        {
            try
            {
                if (_appVeyorBaseUri == null)
                {
                    _appVeyorBaseUri = new Uri(Environment.GetEnvironmentVariable("APPVEYOR_API_URL"));
                }

                var url = new Uri(_appVeyorBaseUri, "api/tests");
                Byte[] byteArray = Encoding.UTF8.GetBytes(json);

                var request = WebRequest.Create(url);
                request.Method = "POST";
                request.ContentLength = byteArray.Length;
                request.ContentType = @"application/json";

                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    //Don't care
                }
            }
            catch
            {
                //Don't care
            }
        }

        private static void PostTestResultToAppveyor(Result result, ResultKind match)
        {
            var fullName = string.Format("{0}.{1}.{2}", result.Test.Name, result.Test.Fixture.Name,
                                         result.Test.Fixture.Assembly.Name);
            var testFramework = string.Format("PclUnit[{0}]", result.Platform);


            string outcome = null;
            switch (match)
            {
                case ResultKind.Success:
                    outcome = "Passed";
                    break;
                case ResultKind.Fail:
                    outcome = "Failed";
                    break;
                case ResultKind.Error:
                    outcome = "NotRunnable";
                    break;
                case ResultKind.Ignore:
                    outcome = "Ignored";
                    break;
                case ResultKind.NoError:
                    outcome = "Inconclusive";
                    break;
            }


            var json = string.Format(@"{{
                                        'testName': '{0}',
                                        'testFramework': '{1}',
                                        'fileName': '{2}',
                                        'outcome': '{3}',
                                        'durationMilliseconds': '{4}',
                                        'ErrorMessage': '',
                                        'ErrorStackTrace': '',
                                        'StdOut': '{5}',
                                        'StdErr': ''  
                                    }}",
                                     fullName.EscapeJson(),
                                     testFramework.EscapeJson(),
                                     result.Test.Fixture.Assembly.Name.EscapeJson(),
                                     outcome.EscapeJson(),
                                     (result.EndTime - result.StartTime).Milliseconds,
                                     result.Output.EscapeJson()
                );

            PostToAppVeyor(json);
        }


        public static void PostMatchResult(PclUnit.Run.Result result, PclUnit.Run.ResultKind match)
        {
             if (_appVeyorRunner)
             {
                 PostTestResultToAppveyor(result, match);
             }
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
                CIResultOutput.WriteLine("##teamcity[testSuiteFinished name='{0}']", Encode(_name));
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