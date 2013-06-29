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

namespace PclUnit.Runner
{

    public enum ResultKind
    {
        NoError = 0,
        Success = 1,
        Ignore = 2,
        Fail = 3,
        Error = 4,
    }

    public class Result:IJsonSerialize
    {

        public static Result Error(TestMeta test, string message, DateTime startTime, DateTime endTime)
        {
            var dummy =new AssertionHelper()
                {
                    Assert = new Assert(),
                    Log = new Log(),
                };

            dummy.Log.Write(message);

            return new Result(test, ResultKind.Error, startTime, endTime, dummy);
        }

        public Result(TestMeta test, ResultKind kind, DateTime startTime, DateTime endTime, IAssertionHelper helper)
        {
            Test = test;
            Kind = kind;
            StartTime = startTime;
            EndTime = endTime;
            Output = helper.Log.ToString();
            AssertCount = helper.Assert.AssertCount;
        }

        public TestMeta Test { get; protected set; }

        public ResultKind Kind { get; protected set; }

        public string Output { get; protected set; }

        public DateTime StartTime { get; protected set; }
        public DateTime EndTime { get; protected set; }
        public int AssertCount { get; protected set; }

        public string ToListJson()
        {
            return ToItemJson();
        }

        public string ToItemJson()
        {
            return String.Format("{{Test:{0}, Kind:\"{1}\", StartTime:\"{4}\",EndTime:\"{5}\",   AssertCount:{2} Output:\"{3}\"}}",
                                 Test.ToItemJson(), Kind, AssertCount, Output.Replace("\"", "\\\""), StartTime.ToString("R"), EndTime.ToString("R"));
        }
    }
}