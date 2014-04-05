
﻿//
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
using PclUnit.Util;

namespace PclUnit.Run
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
        }

        public string Platform { get; set; }

        public ResultKind Kind { get; set; }

        public string Output { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AssertCount { get;  set; }
        public TestMeta Test { get; set; }
        public string ToListJson()
        {
             return String.Format("{{Platform:\"{0}\", Kind:\"{1}\", StartTime:\"{4}\",EndTime:\"{5}\", AssertCount:{2}, Output:\"{3}\"}}",
                                 Platform.EscapeJson(), Kind, AssertCount, Output.EscapeJson(),
                                 StartTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), EndTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt")
                                 );
        }

        public string ToItemJson()
        {
            return String.Format("{{Test:{6}, Platform:\"{0}\", Kind:\"{1}\", StartTime:\"{4}\",EndTime:\"{5}\", AssertCount:{2}, Output:\"{3}\"}}",
                                 Platform.EscapeJson(), Kind, AssertCount, Output.EscapeJson(),
                                 StartTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt"), EndTime.ToString("MM/dd/yyyy hh:mm:ss.fff tt"),
                                 Test.ToItemJson()
                                 );
        }
    }
}
