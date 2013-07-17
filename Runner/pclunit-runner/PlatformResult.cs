// 
//  Copyright 2013 PclUnit Contributors
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.SignalR.Hubs;
using PclUnit.Runner;

namespace pclunit_runner
{
    public class PlatformResult
    {

        public  static readonly List<Result> NoErrors = new List<Result>();
        public static readonly List<Result> Success = new List<Result>();
        public static readonly List<Result> Failures = new List<Result>();
        public static readonly List<Result> Errors = new List<Result>();
        public static readonly List<Result> Ignores = new List<Result>();


        public static IDictionary<string,PlatformResult> AddResult(Result result)
        {
            File.Add(result);

            switch (result.Kind)
            {
       
                case ResultKind.Error:
                    Errors.Add(result);
                    break;
                case  ResultKind.Fail:
                    Failures.Add(result);
                    break;
                case ResultKind.Ignore:
                    Ignores.Add(result);
                    break;       
                case ResultKind.Success:
                    Success.Add(result);
                    break;
                case ResultKind.NoError:
                    NoErrors.Add(result);
                    break;
            }

            string key = string.Format("{0}|{1}|{2}", result.Test.Fixture.Assembly.UniqueName, result.Test.Fixture.UniqueName, result.Test.UniqueName);

            var dict = PlatformResult.ExpectedTests[key].ToDictionary(k => k.Platform, v => v);


            var miniKey = result.Platform;
            dict[miniKey].Result = result;

            return dict;
        }

        public static void AddTest(TestMeta test, string id)
        {
         
            string key = string.Format("{0}|{1}|{2}", test.Fixture.Assembly.UniqueName, test.Fixture.UniqueName,
                                           test.UniqueName);
            lock (ExpectedTests)
            {
                if (!ExpectedTests.ContainsKey(key))
                {
                    ExpectedTests.Add(key, new List<PlatformResult>());
                }
                ExpectedTests[key].Add(new PlatformResult(id) { MissingTest = test });
            }
        }

        private static bool go = false;
        public static void ReceivedTests(string id)
        {
            lock (PlatformResult.WaitingForPlatforms)
            {
                PlatformResult.WaitingForPlatforms.Remove(id);

                if (!PlatformResult.WaitingForPlatforms.Any() && !go)
                {
                    PrintResults.PrintStart();
                    go = true;
                    Clients.All.TestsAreReady(new string[]{});
                }

            }
        }

        public static void Exited(string id)
        {
            lock (PlatformResult.WaitingForPlatforms)
            {
                if (WaitingForPlatforms.Contains(id))
                {
                    AddTest(TestMeta.FakeTest("Never Received Tests"), id);

                }
            }

            lock (ExpectedTests)
            {
                foreach (var expectedTest in ExpectedTests)
                {
                    var result = expectedTest.Value.FirstOrDefault(it => it.Platform == id);

                    if (result != null && result.Result == null)
                    {
                        AddResult(new Result()
                                      {
                                          Platform = id,
                                          Kind = ResultKind.Error,
                                          Output = "Runner unexpectedly quit",
                                          Test = result.MissingTest
                                      });
                    }
                }
            }

            ReceivedTests(id);
        }

        public PlatformResult(string platform)
        {
            Platform = platform;
        }
        public string Platform { get; set; }
        public Result Result { get; set; }

        public TestMeta MissingTest { get; set; }

        public static HubConnectionContext Clients { get; set; }

        public static readonly IDictionary<string, IList<PlatformResult>> ExpectedTests =
            new Dictionary<string, IList<PlatformResult>>();

        public static readonly ResultsFile File = new ResultsFile();

        public static readonly HashSet<string> WaitingForPlatforms =
            new HashSet<string>();
    }
}