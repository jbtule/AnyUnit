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
using System.Reflection;

namespace PclUnit.Run
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

            if (Test.OfType<IgnoreException>().Any())
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
                    WriteOutFullExceptionHelper(helper, ex);
            }
        }
    }
}
