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
using System.Text;
using AnyUnit.Run;
using TestDriven.Framework;

namespace AnyUnit.TestDriven.NetOnly
{
    public class AnyUnitRunner:ITestRunner
    {
        public TestRunState RunAssembly(ITestListener testListener, Assembly assembly)
        {

            return Run(testListener, Runner.Create("td-net", new[] { assembly }), null);
        }

        private TestRunState Run( ITestListener testListener, Runner runner, TestFilter filter)
        {

           var state = TestRunState.NoTests;


                     runner.RunAll(result =>{ 
                         
                            testListener.TestFinished( new TestResult()
                                                                    {
                                                                        Name = String.Format("{0}.{1}",result.Test.Fixture.Name, result.Test.Name),
                                                                        FixtureType = ((Fixture)result.Test.Fixture).Type,
                                                                        StackTrace = result.Output,
                                                                        Method = ((Test)result.Test).Method,
                                                                        State = StateForResult(result.Kind),
                                                                        TimeSpan = result.EndTime - result.StartTime,
                                                                    });
                     
                                    if(state == TestRunState.NoTests){
                                        switch (result.Kind)
                                        {
                                            case ResultKind.Fail:
                                                state = TestRunState.Failure;
                                                break;
                                            case ResultKind.Error:
                                                state = TestRunState.Error;
                                                break;
                                            default:
                                                state = TestRunState.Success;
                                                break;
                                        }
                                    } else if(state == TestRunState.Success || state == TestRunState.Failure){
                                        switch (result.Kind)
                                        {
                                            case ResultKind.Fail:
                                                state = TestRunState.Failure;
                                                break;
                                            case ResultKind.Error:
                                                state = TestRunState.Error;
                                                break;
                                        }
                                    }


                     }, filter);
            return state;
        }

        private TestState StateForResult(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Success:
                case ResultKind.NoError:
                    return TestState.Passed;
                case ResultKind.Ignore:
                    return TestState.Ignored;
                default:
                    return TestState.Failed;
            }
        }

        public TestRunState RunNamespace(ITestListener testListener, Assembly assembly, string ns)
        {
            return Run(testListener,
                Runner.Create("td-net", new[] { assembly }),
                new TestFilter(new[] { String.Format("T:{0}", ns) }, new string[] { }));
        }

        public TestRunState RunMember(ITestListener testListener, Assembly assembly, MemberInfo member)
        {
            var type = member as Type;  
            var method = member as MethodInfo;
            var testFilter = new TestFilter();

            var runner =Runner.Create("td-net", new[] {assembly});

            if (type != null)
            {
                
                foreach (var fixture in runner.Assemblies.SelectMany(a => a.Fixtures).OfType<Fixture>())
                {
                    if (fixture.Type == type)
                    {
                        testFilter.Includes.Add(fixture.UniqueName);
                    }
                }
            }else if (method != null)
            {
            
                foreach (var test in runner.Assemblies.SelectMany(a => a.Fixtures).SelectMany(f=>f.Tests).OfType<Test>())
                {
                    if (test.Method == method)
                    {
                        testFilter.Includes.Add(test.UniqueName);
                    }
                }

            }
            else
            {
                return TestRunState.NoTests;
            }

            return Run(testListener, runner, testFilter);
            
        }
    }
}
