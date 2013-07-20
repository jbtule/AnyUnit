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
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PclUnit.Run
{
    public class Test : TestMeta
    {

        public static void Sleep(int milliseconds)
        {
            WaitHandle.WaitAll(new[] { new ManualResetEvent(false) }, milliseconds);
        }

        private readonly FixtureInitializer _init;
        private readonly Type _type;
        private readonly TestInvoker _invoke;
        private readonly ParameterSet _constructorArgs;
        private readonly MethodInfo _method;
        private readonly ParameterSet _methodArgs;

        public Test(Fixture fixture, ParameterSet constructorArgs, TestHarness harness, ParameterSet methodArgs)
        {
            Category = harness.Category;
            Description = harness.Description;
            if (Timeout != System.Threading.Timeout.Infinite)
            {
                Timeout = harness.Timeout;
            }


            UniqueName = string.Format("M:{0}.{1}", harness.Method.DeclaringType.Namespace, harness.Method.DeclaringType.Name);

            Name = String.Empty;
            if (constructorArgs.Parameters.Any())
            {
                var nameArgs = constructorArgs.Parameters.Select(it => it.ToString()).ToList();

                UniqueName += string.Format("({0})[{1}]", String.Join(",", nameArgs.ToArray()), constructorArgs.Index);

                Name += string.Format("({0})", String.Join(",", nameArgs.ToArray()));
            }



            UniqueName += "." + harness.Method.Name;
            Name += harness.Method.Name;

            if (methodArgs.Parameters.Any())
            {
                var nameArgs = methodArgs.Parameters.Select(it => it.ToString());

                UniqueName += string.Format("({0})[{1}]", String.Join(",", nameArgs.ToArray()), constructorArgs.Index);

                Name += string.Format("({0})", String.Join(",", nameArgs.ToArray()));
            }

            _init = fixture.Attribute.FixtureInit;
            _type = fixture.Type;
            _invoke = harness.Attribute.TestInvoke;
            _constructorArgs = constructorArgs.Retain();
            _method = harness.Method;
            _methodArgs = methodArgs.Retain();
        }

        internal class State
        {
            public State(string platform)
            {
               Platform = platform;
               Event = new ManualResetEvent(false);
            }
            public string Platform { get; protected set; }
            public Result Result { get; set; }
            public ManualResetEvent Event { get; protected set; }
        }

        public Result Run(string platform)
        {
            var state = new State(platform);
            var startTime = DateTime.Now;
            ThreadPool.QueueUserWorkItem(RunHelper, state);
            Result result =null;
            if (WaitHandle.WaitAll(new WaitHandle[] {state.Event}, Timeout ?? System.Threading.Timeout.Infinite))
            {
                ParameterSetRelease();
                result = state.Result;
            }
            else
            {
                ParameterSetRelease();
                result = Result.Error(platform, "Tests Execution Timed Out", startTime, DateTime.Now);
            }
            Results.Add(result);
            return result;

        }

        public void ParameterSetRelease()
        {
            _constructorArgs.Release();
            _methodArgs.Release();
        }

        private void RunHelper(Object stateInfo)
        {
            var state = (State) stateInfo;
            var startTime = DateTime.Now;

         


            var fixture = _init(_type, _constructorArgs.Parameters);

            var helper = fixture as IAssertionHelper ?? new DummyHelper();
            helper.Assert = new Assert();
            helper.Log = new Log();
            Result returnVal =null;
            using (fixture as IDisposable)
            {
                try
                {

                    if (_constructorArgs.Disposed)
                    {
                        throw new InvalidOperationException("Type ParameterSet Disposed Regenerated Tests to rerun");
                    }
                    if (_methodArgs.Disposed)
                    {
                        throw new InvalidOperationException("Method ParameterSet Disposed Regenerated Tests to rerun");
                    }

                    var result = _invoke(_method, fixture, _methodArgs.Parameters);

                    //If the test method returns a boolean, true increments assertion
                    if (result as bool? ?? false)
                    {
                        helper.Assert.Okay();
                    }

                    //If the test method returns a boolean, false is fail
                    if (!(result as bool? ?? true))
                    {
                        helper.Assert.Fail("Test returned false.");
                    }

                    if (!(helper is DummyHelper) && !Assert._globalStyleUsed && helper.Assert.AssertCount == 0)
                    {
                        returnVal = new Result(state.Platform, ResultKind.NoError, startTime, DateTime.Now, helper);
                    }
                    else
                    {
                        returnVal = new Result(state.Platform, ResultKind.Success, startTime, DateTime.Now, helper);
                    }
                }
                    //Reflection wraps exceptions with target exceptions
                catch (Exception ex)
                {

                    if (ex is TargetInvocationException)
                    {
                        ex = ex.InnerException ?? ex;
                    }

                    if (ex is AssertionException)
                    {
                        helper.Log.WriteLine(ex.Message);
                        helper.Log.WriteLine(ex.StackTrace);

                        returnVal = new Result(state.Platform, ResultKind.Fail, startTime, DateTime.Now, helper);

                    }
                    else if (ex is IgnoreException)
                    {
                        helper.Log.Write(ex.Message);
                        returnVal = new Result(state.Platform, ResultKind.Ignore, startTime, DateTime.Now, helper);
                    }
                    else
                    {


                        helper.Log.Write("{0}: ", ex.GetType().Name);
                        helper.Log.WriteLine(ex.Message);
                        helper.Log.WriteLine(ex.StackTrace);


                        returnVal = new Result(state.Platform, ResultKind.Error, startTime, DateTime.Now, helper);
                    }
                }
                finally
                {
                    state.Result = returnVal;
                    state.Event.Set();
                }
                
              
            }
        }
    }
}