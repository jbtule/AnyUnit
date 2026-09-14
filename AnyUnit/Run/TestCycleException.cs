//
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnyUnit.Run
{
    public enum TestCycle
    {
        Setup,
        Test,
        Teardown,
    }

    public class TestCycleExceptions:Exception
    {
        protected IList<Exception> Setup { get; set; }
        protected IList<Exception> Test { get; set; }
        protected IList<Exception> Teardown { get; set; }



        public TestCycleExceptions()
        {
            Setup = new List<Exception>();
            Test = new List<Exception>();
            Teardown = new List<Exception>();
        }



        public void Add(TestCycle cycle, Exception exception)
        {
            var testException = exception as TestCycleExceptions;
            if (testException != null)
            {
                Add(testException);
                return;
            }

            //Unwrap Target invocation Exceptions if possible
            if (exception is TargetInvocationException)
            {
                exception = exception.InnerException ?? exception;
            }

            switch (cycle)
            {
                case TestCycle.Setup:
                    Setup.Add(exception);
                    break;
                case TestCycle.Test:
                    Test.Add(exception);
                    break;
                case TestCycle.Teardown:
                    Teardown.Add(exception);
                    break;
            }
        }

        public void Add(TestCycleExceptions exceptions)
        {
            foreach (var ex in exceptions.Setup)
            {
                Setup.Add(ex);
            }

            foreach (var ex in exceptions.Test)
            {
                Test.Add(ex);
            }

            foreach (var ex in exceptions.Teardown)
            {
                Teardown.Add(ex);
            }
        }

        public ResultKind GetResult(IAssertionHelper helper)
        {

            // An IgnoreException can come from fixture construction (a
            // class-level [Ignore]/[Platform]), [SetUp], an ITestAction's
            // BeforeTest, the test body itself, or teardown-side hooks -
            // wherever it's thrown from, it means the test was skipped,
            // not that something errored.
            if (Setup.OfType<IgnoreException>().Any()
                || Test.OfType<IgnoreException>().Any()
                || Teardown.OfType<IgnoreException>().Any())
                return ResultKind.Ignore;

            if(Setup.Any()
               || Teardown.Any()
               || Test.Any(it => !(it is ResultException)))
                return ResultKind.Error;

            if(Test.OfType<AssertionException>().Any())
                return ResultKind.Fail;

            if (!Assert._globalStyleUsed && helper.Assert.AssertCount == 0)
            {
                return ResultKind.NoError;
            }

            return ResultKind.Success;

        }

        // The single exception a report format should attribute the result
        // to - the one whose Message/StackTrace/type name become
        // Result.Message/StackTrace/ExceptionType. Deliberately sits right
        // next to GetResult and mirrors its precedence branch for branch,
        // so the two can't drift into disagreeing about which exception
        // actually decided the outcome. Output is unaffected and still
        // carries every exception (see WriteOutExceptions below), so the
        // multi-exception case - setup and teardown both throwing - loses
        // nothing by this picking one.
        //
        // Returns null when nothing went wrong, and also for Success /
        // NoError, which have no exception by definition.
        public Exception GetPrimaryException(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Ignore:
                    // Same Setup -> Test -> Teardown order GetResult tests
                    // its IgnoreExceptions in.
                    return Setup.OfType<IgnoreException>().FirstOrDefault()
                        ?? Test.OfType<IgnoreException>().FirstOrDefault()
                        ?? (Exception)Teardown.OfType<IgnoreException>().FirstOrDefault();

                case ResultKind.Error:
                    // GetResult's Error branch is
                    // `Setup.Any() || Teardown.Any() || Test.Any(not a
                    // ResultException)`. Setup first because a failed setup
                    // is why nothing else could work; a non-ResultException
                    // from the test body next (a ResultException is an
                    // assertion outcome, not an error); teardown last.
                    return Setup.FirstOrDefault()
                        ?? Test.FirstOrDefault(it => !(it is ResultException))
                        ?? Teardown.FirstOrDefault();

                case ResultKind.Fail:
                    return Test.OfType<AssertionException>().FirstOrDefault();

                default:
                    return null;
            }
        }

        public void WriteOutExceptions(IAssertionHelper helper)
        {
            if (Setup.Any())
            {
                helper.Log.WriteLine("Set Up Error:");
                foreach (var exception in Setup)
                {
                    helper.Log.Indent();
                    WriteOutFullExceptionHelper(helper, exception);
                    helper.Log.UnIndent();
                }
            }

            if (Test.Any())
            {
                foreach (var exception in Test)
                {
                    WriteOutFullExceptionHelper(helper, exception);
                }
                helper.Log.WriteLine(String.Empty);
            }

            if (Teardown.Any())
            {
                helper.Log.WriteLine("Tear Down Error:");
                foreach (var exception in Teardown)
                {
                    helper.Log.Indent();
                    WriteOutFullExceptionHelper(helper, exception);
                    helper.Log.UnIndent();
                }
            }
        }

        private void WriteOutFullExceptionHelper(IAssertionHelper helper, Exception ex)
        {

            if (ex is AssertionException)
            {
                helper.Log.WriteLine(ex.Message);
                helper.Log.WriteLine(ex.StackTrace);
            }
            else if (ex is IgnoreException)
            {
                helper.Log.Write(ex.Message);
            }
            else
            {
                helper.Log.Write("{0}: ", ex.GetType().Name);
                helper.Log.WriteLine(ex.Message);
                helper.Log.WriteLine(ex.StackTrace);
                if (ex.InnerException != null)
                    WriteOutFullExceptionHelper(helper, ex.InnerException);
            }
        }
    }
}
