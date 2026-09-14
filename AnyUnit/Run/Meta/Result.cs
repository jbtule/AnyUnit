
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

            var result = new Result(platform, ResultKind.Error, startTime, endTime, dummy);
            // The only caller is Test.Run's [Timeout] path, whose message
            // ("Tests Execution Timed Out") is a genuine failure reason and
            // not captured log output - before this it only ever reached
            // Log, so every report format had to present the timeout as if
            // it were the test's own console output.
            result.Message = message;
            return result;
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

        // Same as the constructor above, plus the exceptions that actually
        // decided `kind`. Kept as a separate overload rather than an extra
        // parameter on the existing one because IReturnedResult-based
        // results and Result.Error() above genuinely have no
        // TestCycleExceptions to hand, and because everything already
        // calling the 5-arg form keeps working unchanged.
        public Result(string platform, ResultKind kind, DateTime startTime, DateTime endTime, IAssertionHelper helper,
                      TestCycleExceptions exceptions)
            : this(platform, kind, startTime, endTime, helper)
        {
            var primary = exceptions.Maybe(it => it.GetPrimaryException(kind));
            if (primary == null)
                return;

            if (kind == ResultKind.Ignore)
            {
                // An IgnoreException's message IS the skip reason - there
                // is no separate failure to report, so Message/StackTrace/
                // ExceptionType stay null rather than duplicating it.
                SkipReason = primary.Message;
            }
            else
            {
                Message = primary.Message;
                // Genuinely nullable, not merely usually-present: an
                // exception that was constructed but never thrown has no
                // stack trace at all, and assertion libraries that fold the
                // location into the message text (TUnit does exactly this)
                // emit an empty one even when thrown.
                StackTrace = primary.StackTrace;
                ExceptionType = primary.GetType().FullName;
            }
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

        // The whole captured log for this test - everything written through
        // ILog, including the flattened text of every exception. The four
        // fields below are NOT subsets carved out of it; they come straight
        // off the exception object that decided the outcome, which the
        // runner used to flatten into Output and then discard. Output stays
        // exactly what it always was.
        public string Output { get; set; }

        // The deciding exception's own message. Null on a passing test, on
        // a skip (see SkipReason), and on a result that came back as an
        // IReturnedResult - those carry no exception at all. Must serialize
        // as bare null rather than "" so writers can tell "this file
        // predates the field" from "this test had an empty message" and
        // fall back to Output only in the first case.
        public string Message { get; set; }

        // The deciding exception's stack trace. Independently nullable from
        // Message - see the constructor overload above.
        public string StackTrace { get; set; }

        // The deciding exception's type FullName, e.g.
        // "AnyUnit.AssertionException" or
        // "System.InvalidOperationException". The only thing any report
        // format carries that distinguishes an assertion failure from an
        // unexpected throw - TRX collapses both to "Failed".
        public string ExceptionType { get; set; }

        // Why an Ignore result was skipped, as its own value rather than as
        // text inside Output. Null unless Kind == ResultKind.Ignore. Every
        // report format has a dedicated slot for this (JUnit's
        // <skipped message=>, NUnit3's <reason><message>, xUnit's <reason>,
        // TRX's ErrorInfo/Message under NotExecuted) - one source field,
        // four destinations.
        public string SkipReason { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int AssertCount { get;  set; }
        public string OSDescription { get; set; }
        public string OSArchitecture { get; set; }
        public string FrameworkDescription { get; set; }
        public TestMeta Test { get; set; }
        // NOTE: there are TWO independent serializers here, and every field
        // has to be added to both. ToItemJson is not ToListJson plus the
        // parent - it uses its own field order and its own positional
        // argument numbering, so the two format strings share nothing a
        // compiler could check. The four new fields' placeholders are
        // deliberately unquoted (\"Message\":{9}, not \"Message\":\"{9}\")
        // because ToJsonStringOrNull supplies the quotes, or the bare
        // literal null - see its comment in AnyUnit.Util.Utility.
        public string ToListJson()
        {
             return String.Format("{{\"Platform\":\"{0}\", \"Kind\":\"{1}\", \"StartTime\":\"{4}\",\"EndTime\":\"{5}\", \"AssertCount\":{2}, \"Output\":\"{3}\", \"Message\":{9}, \"StackTrace\":{10}, \"ExceptionType\":{11}, \"SkipReason\":{12}, \"OSDescription\":\"{6}\", \"OSArchitecture\":\"{7}\", \"FrameworkDescription\":\"{8}\"}}",
                                 Platform.EscapeJson(), Kind, AssertCount, Output.EscapeJson(),
                                 StartTime.ToString("o"), EndTime.ToString("o"),
                                 OSDescription.EscapeJson(), OSArchitecture.EscapeJson(), FrameworkDescription.EscapeJson(),
                                 Message.ToJsonStringOrNull(), StackTrace.ToJsonStringOrNull(),
                                 ExceptionType.ToJsonStringOrNull(), SkipReason.ToJsonStringOrNull()
                                 );
        }

        public string ToItemJson()
        {
            return String.Format("{{\"Test\":{6}, \"Platform\":\"{0}\", \"Kind\":\"{1}\", \"StartTime\":\"{4}\",\"EndTime\":\"{5}\", \"AssertCount\":{2}, \"Output\":\"{3}\", \"Message\":{10}, \"StackTrace\":{11}, \"ExceptionType\":{12}, \"SkipReason\":{13}, \"OSDescription\":\"{7}\", \"OSArchitecture\":\"{8}\", \"FrameworkDescription\":\"{9}\"}}",
                                 Platform.EscapeJson(), Kind, AssertCount, Output.EscapeJson(),
                                 StartTime.ToString("o"), EndTime.ToString("o"),
                                 Test.ToItemJson(),
                                 OSDescription.EscapeJson(), OSArchitecture.EscapeJson(), FrameworkDescription.EscapeJson(),
                                 Message.ToJsonStringOrNull(), StackTrace.ToJsonStringOrNull(),
                                 ExceptionType.ToJsonStringOrNull(), SkipReason.ToJsonStringOrNull()
                                 );
        }
    }
}
