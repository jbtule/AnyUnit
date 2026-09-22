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
using System.Linq;
using System.Reflection;
using System.Threading;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;
using AnyUnit.Compat.NetStandardV1;

namespace AnyUnit.Run
{
    public class Test : TestMeta
    {

        public static void Sleep(int milliseconds)
        {
            if (Utility.IsSingleThreadedRuntime)
            {
                // Same reasoning as the single-threaded branch in Run() below:
                // a blocking wait needs a real second thread to eventually
                // unblock it (even one that's only there to honor the
                // timeout), which single-threaded WASM doesn't have. Rather
                // than hang, just don't block - callers busy-looping on
                // wall-clock time around this (e.g. AnyUnit's [Timeout]
                // tests) will simply spin faster, not slower.
                return;
            }
            WaitHandle.WaitAll(new[] { new ManualResetEvent(false) }, milliseconds);
        }

        private readonly Fixture _fixture;
        private readonly FixtureInitializer _init;
        private readonly Type _type;
        private readonly TestInvoker _invoke;
        private readonly ParameterSet _constructorArgs;
        private readonly MethodInfo _method;
        private readonly ParameterSet _methodArgs;
        private readonly TestCapabilities _requiredCapabilities;

        // Prefix on the IgnoreException message for a capability skip, so a
        // consumer can tell "this platform can't run it" apart from "the
        // suite chose to ignore it" - the two are both ResultKind.Ignore
        // and would otherwise be indistinguishable. Shared as a const
        // rather than duplicated: WhoTestsTheTesters/ConventionTestProcessor
        // matches on it to accept such a skip whatever outcome the test's
        // name encodes.
        public const string CapabilityUnavailablePrefix = "Capability unavailable:";

        public MethodInfo Method
        {
            get { return _method; }
        }

        public Test(Fixture fixture, ParameterSet constructorArgs, TestHarness harness, ParameterSet methodArgs)
        {
            Category = harness.Category;
            Properties = harness.Properties ?? Properties;
            Description = harness.Description;
            if (Timeout != System.Threading.Timeout.Infinite)
            {
                Timeout = harness.Timeout;
            }


            UniqueName = string.Format("M:{0}.{1}", harness.Method.DeclaringType.Namespace, harness.Method.DeclaringType.Name);

            Name = String.Empty;
            // it?.ToString() ?? "null": a real, common test case (e.g. NUnit's
            // [TestCase(null)], testing how a method handles a null argument)
            // has a genuinely null element in Parameters - ToString() on it
            // directly used to NRE building this test's display name, before
            // the test itself ever got a chance to run.
            if (constructorArgs.Parameters.Any())
            {
                var nameArgs = constructorArgs.Parameters.Select(it => it?.ToString() ?? "null").ToList();

                UniqueName += string.Format("({0})[{1}]", String.Join(",", nameArgs.ToArray()), constructorArgs.Index);

                Name += string.Format("({0})", String.Join(",", nameArgs.ToArray()));
            }



            // DisplayName, when a style set one, stands in for the method
            // name in BOTH halves - not just the pretty one. A style that
            // produces several tests from a single member (Expecto's
            // `testList "a" [ testCase "b" ...; testCase "c" ... ]` is one
            // F# `let` binding, so one PropertyInfo, yielding many leaves)
            // would otherwise give every one of them the same UniqueName,
            // and ResultsFile.Add - which keys each level by UniqueName -
            // would silently merge them into a single test.
            var memberName = string.IsNullOrEmpty(harness.DisplayName)
                ? harness.Method.Name
                : harness.DisplayName;

            UniqueName += "." + memberName;
            Name += memberName;

            if (methodArgs.Parameters.Any())
            {
                var nameArgs = methodArgs.Parameters.Select(it => it?.ToString() ?? "null");

                UniqueName += string.Format("({0})[{1}]", String.Join(",", nameArgs.ToArray()), constructorArgs.Index);

                Name += string.Format("({0})", String.Join(",", nameArgs.ToArray()));
            }

            // Fixture-level requirements union with the test's own: a
            // fixture that needs a facility needs it for every test in it.
            _requiredCapabilities = harness.RequiredCapabilities
                                    | fixture.Attribute.GetRequiredCapabilities(fixture.Type);

            _fixture = fixture;
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

            if (Utility.IsSingleThreadedRuntime)
            {
                // See Utility.IsSingleThreadedRuntime: the ThreadPool +
                // blocking-wait dance below would deadlock here, so run the
                // test body inline instead. This does mean [Timeout] isn't
                // enforced on this platform - a hung test hangs the caller,
                // same as it would hang any other single-threaded caller.
                RunHelper(state);
                ParameterSetRelease();
                Results.Add(state.Result);
                return state.Result;
            }

            Utility.RunThreadWithState(RunHelper, state);
            Result result;
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



            IAssertionHelper helper = new DummyHelper()
                                          {
                                              Assert = new Assert(),
                                              Log = new Log()
                                          };

            object fixture = null;
            var exceptions = new TestCycleExceptions();
            Result finalResult = null;
            try
            {
                // Outermost-first: a namespace-scoped [SetUpFixture] wraps
                // its own fixture-level OneTimeSetUp.
                foreach (var scope in _fixture.ApplicableNamespaceScopes)
                {
                    scope.EnsureOneTimeSetUp();
                }

                _fixture.EnsureOneTimeSetUp();

                fixture = _init(_type, _constructorArgs.Parameters);
                var helpertemp = fixture as IAssertionHelper;
                if (helpertemp != null)
                {
                    helpertemp.Assert = helper.Assert;
                    helpertemp.Log = helper.Log;
                    helper = helpertemp;
                }

                if (_constructorArgs.Disposed)
                {
                    throw new InvalidOperationException("Type ParameterSet Disposed Regenerated Tests to rerun");
                }
                if (_methodArgs.Disposed)
                {
                    throw new InvalidOperationException(
                        "Method ParameterSet Disposed Regenerated Tests to rerun");
                }

                try
                {
                    if (_methodArgs.IgnoreReason != null)
                    {
                        throw new IgnoreException(_methodArgs.IgnoreReason);
                    }

                    // Checked here rather than filtered out by each host:
                    // every runner honours it for free, and - unlike a
                    // host-side filter, which makes the test simply vanish
                    // from the results - it leaves a reported Ignore saying
                    // exactly which facility was missing.
                    var missing = PlatformCapabilities.Missing(_requiredCapabilities);
                    if (missing != TestCapabilities.None)
                    {
                        throw new IgnoreException(string.Format(
                            "{0} this test requires {1}, which is not available on this platform.",
                            CapabilityUnavailablePrefix, missing));
                    }

                    // The running test, for anything that asserts without
                    // a `this` to go through - see AmbientTest. Wrapped
                    // around the unwrap too, not just the call: an async
                    // body is still running until Unwrap says otherwise.
                    object result;
                    AmbientTest.Enter(helper);
                    try
                    {
                        result = _invoke(helper, _method, fixture, _methodArgs.Parameters);

                        // An async test method hands back a Task (or ValueTask,
                        // or an F# Async) that is very possibly not finished yet,
                        // and that holds any exception the body raised instead of
                        // throwing it out of the call above. Wait for it here, so
                        // the bool/IReturnedResult inspection below sees the real
                        // produced value and the catch below sees the real
                        // exception. See AsyncTestResult for the whole story.
                        result = AsyncTestResult.Unwrap(result);
                    }
                    finally
                    {
                        AmbientTest.Exit();
                    }

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

                    if (result is IReturnedResult)
                    {
                       finalResult = new Result(state.Platform,
                                                  startTime, DateTime.Now, result as IReturnedResult);
                    }
                }
                catch (Exception ex)
                {
                     exceptions.Add(TestCycle.Test, ex);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(TestCycle.Setup, ex);
            }
            finally
            {
                try
                {
                    var disposable = fixture as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(TestCycle.Teardown, ex);
                }
                try
                {
                    _fixture.NotifyTestFinished();
                }
                catch (Exception ex)
                {
                    exceptions.Add(TestCycle.Teardown, ex);
                }
                // Innermost-first: the reverse of the setup-side order above.
                // Each scope is notified independently - one throwing must
                // not stop the others from being notified too, or their
                // own countdowns would never reach zero.
                for (var i = _fixture.ApplicableNamespaceScopes.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        _fixture.ApplicableNamespaceScopes[i].NotifyTestFinished();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(TestCycle.Teardown, ex);
                    }
                }
                exceptions.WriteOutExceptions(helper);
                // `exceptions` is handed to the Result too, not just to
                // WriteOutExceptions above: the real Exception objects are
                // still in scope right here, and before this they were
                // flattened into the log and then dropped, leaving every
                // report writer with nothing but Output to put in both the
                // message and the stack-trace slot.
                state.Result = finalResult ?? new Result(state.Platform, exceptions.GetResult(helper), startTime, DateTime.Now, helper, exceptions);
                state.Event.Set();
            }

        }
    }
}
