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
using PclUnit.Util;

namespace PclUnit.Runner
{
    public class Test : TestMeta
    {
        private readonly Type _type;
        private readonly object[] _constructorArgs;
        private readonly MethodInfo _method;
        private readonly object[] _methodArgs;

        public Test(TestAttribute attribute, Type type, object[] constructorArgs, MethodInfo method, object[] methodArgs)
        {
            Category = attribute.Category.SafeSplit(",").ToList();
            Description = attribute.Description;
          
            UniqueName = "M:" + method.DeclaringType.Namespace + "." + method.DeclaringType.Name;

            Name = String.Empty;
            if (constructorArgs.Any())
            {
                var uniqueargs =constructorArgs.Select(it => it.GetType().ToString() +"#" + it.GetHashCode().ToString());

                UniqueName += string.Format("({0})", String.Join(",", uniqueargs.ToArray()));

                var nameArgs = constructorArgs.Select(it => it.ToString());

                Name += string.Format("({0})", String.Join(",", nameArgs.ToArray()));
            }

           

            UniqueName += "." + method.Name;
            Name += method.Name;

            if (methodArgs.Any())
            {
                var uniqueargs = methodArgs.Select(it => it.GetType().ToString() + "#" + it.GetHashCode().ToString());

                UniqueName += string.Format("({0})", String.Join(",", uniqueargs.ToArray()));

                var nameArgs = methodArgs.Select(it => it.ToString());

                Name += string.Format("({0})", String.Join(",", nameArgs.ToArray()));
            }
             
         

            _type = type;
            _constructorArgs = constructorArgs;
            _method = method;
            _methodArgs = methodArgs;
        }

        public Result Run()
        {

            var fixture = Activator.CreateInstance(_type, _constructorArgs);

            var helper = fixture as IAssertionHelper ?? new DummyHelper();
            helper.Assert = new Assert();
            helper.Log = new Log();

            using (fixture as IDisposable)
            {
                try
                {
                    var result = _method.Invoke(fixture, _methodArgs);

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

                    if (!(helper is DummyHelper) && helper.Assert.AssertCount == 0)
                    {
                        return new Result(this, ResultKind.NoError, helper);
                    }

                    return new Result(this, ResultKind.Success, helper);
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

                        return new Result(this, ResultKind.Fail, helper);

                    }
                    if (ex is IgnoreException)
                    {
                        helper.Log.Write(ex.Message);
                        return new Result(this, ResultKind.Ignore, helper);
                    }
                    

                    helper.Log.Write("{0}: ", ex.GetType().Name);
                    helper.Log.WriteLine(ex.Message);
                    helper.Log.WriteLine(ex.StackTrace);

                    return new Result(this, ResultKind.Error, helper);
                }
            }
        }
    }
}