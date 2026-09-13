
﻿//
//  Copyright 2013 AnyUnit Contributors
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
using System.Runtime.InteropServices;
using AnyUnit.Util;

namespace AnyUnit.Run
{

    public enum ResultKind
    {
        NoError = 0,
        Success = 1,
        Ignore = 2,
        Fail = 3,
        Error = 4,
    }

    public interface IReturnedResult {

        ResultKind Kind { get; set; }
        int AssertCount { get; set; }
        string Output { get; set; }
    }

    public class Result:IJsonSerialize,IReturnedResult
    {

        public static Result Error(string platform, string message, DateTime startTime, DateTime endTime)
        {
            var dummy =new AssertionHelper()
                {
                    Assert = new Assert(),
                    Log = new Log(),
                };

            dummy.Log.Write(message);

            return new Result(platform, ResultKind.Error, startTime, endTime, dummy);
        }

        public Result()
        {

        }

        public Result(string platform, DateTime startTime, DateTime endTime, IReturnedResult returnedResult)
        {

            Platform = platform;
            StartTime = startTime;
            EndTime = endTime;
            Kind = returnedResult.Kind;
            AssertCount = returnedResult.AssertCount;
            Output = returnedResult.Output;
            SetEnvironment();
        }

        public Result(string platform, ResultKind kind, DateTime startTime, DateTime endTime, IAssertionHelper helper)
        {
            Platform = platform;
            Kind = kind;
            StartTime = startTime;
            EndTime = endTime;
            Output = helper.Log.ToString();
            if (Assert._globalStyleUsed && helper.Assert.AssertCount == 0)
                AssertCount = -1;
            else
                AssertCount = helper.Assert.AssertCount;
            SetEnvironment();
        }

        // OS/runtime this Result was actually produced under - captured
        // here, not just once globally, since a single merged results.json
        // (see AnyUnit.Report's multi-input convert) can genuinely combine
        // Results from different real environments (a net10 run and a
        // net48 run are not the same OS/runtime just because they're in
        // the same file). netstandard2.0-safe subset of RuntimeInformation
        // only (no RuntimeIdentifier - that member needs netstandard2.1+,
        // and this library also ships to net48).
        private void SetEnvironment()
        {
            OSDescription = RuntimeInformation.OSDescription;
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString();
            FrameworkDescription = RuntimeInformation.FrameworkDescription;
        }

        public string Platform { get; set; }

        public ResultKind Kind { get; set; }

        public string Output { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AssertCount { get;  set; }
        public string OSDescription { get; set; }
        public string OSArchitecture { get; set; }
        public string FrameworkDescription { get; set; }
        public TestMeta Test { get; set; }
        public string ToListJson()
        {
             return String.Format("{{\"Platform\":\"{0}\", \"Kind\":\"{1}\", \"StartTime\":\"{4}\",\"EndTime\":\"{5}\", \"AssertCount\":{2}, \"Output\":\"{3}\", \"OSDescription\":\"{6}\", \"OSArchitecture\":\"{7}\", \"FrameworkDescription\":\"{8}\"}}",
                                 Platform.EscapeJson(), Kind, AssertCount, Output.EscapeJson(),
                                 StartTime.ToString("o"), EndTime.ToString("o"),
                                 OSDescription.EscapeJson(), OSArchitecture.EscapeJson(), FrameworkDescription.EscapeJson()
                                 );
        }

        public string ToItemJson()
        {
            return String.Format("{{\"Test\":{6}, \"Platform\":\"{0}\", \"Kind\":\"{1}\", \"StartTime\":\"{4}\",\"EndTime\":\"{5}\", \"AssertCount\":{2}, \"Output\":\"{3}\", \"OSDescription\":\"{7}\", \"OSArchitecture\":\"{8}\", \"FrameworkDescription\":\"{9}\"}}",
                                 Platform.EscapeJson(), Kind, AssertCount, Output.EscapeJson(),
                                 StartTime.ToString("o"), EndTime.ToString("o"),
                                 Test.ToItemJson(),
                                 OSDescription.EscapeJson(), OSArchitecture.EscapeJson(), FrameworkDescription.EscapeJson()
                                 );
        }
    }
}
