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
using AnyUnit.Run;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// Marks a class whose [OneTimeSetUp]/[OneTimeTearDown] methods run
    /// once before/after every test in this class's namespace (and its
    /// sub-namespaces) - not just this one class's own tests, unlike the
    /// same attributes on a [TestFixture]. See AnyUnit.Run.NamespaceScope
    /// for how that's tracked.
    ///
    /// Unlike a per-test fixture (see Test.RunHelper), nothing previously
    /// injected Assert/Log into an instance-based [SetUpFixture] - if it
    /// implemented IAssertionHelper, Assert/Log just stayed null. Now
    /// mirrors the per-test pattern: a real Assert/Log get set before
    /// invoking, and since there's no per-test Result for one-time setup
    /// to attach a captured Log to, it's flushed straight to Console
    /// immediately after (a one-time, suite-startup print, not a per-test
    /// one - the same "no Result to attach to" reasoning that makes plain
    /// console output the right call here, not a limitation).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SetUpFixtureAttribute : SetUpFixtureAttributeBase
    {
        public override FixtureOneTimeSetUpAction OneTimeSetUp
        {
            get
            {
                return type =>
                           {
                               var method = GetOneTimeMethod(type, typeof (OneTimeSetUpAttribute));
                               if (method == null)
                                   return null;

                               object instance = method.IsStatic ? null : Activator.CreateInstance(type);
                               var helper = instance as IAssertionHelper;
                               Log log = null;
                               if (helper != null)
                               {
                                   log = new Log();
                                   helper.Assert = new Assert();
                                   helper.Log = log;
                               }
                               try
                               {
                                   method.Invoke(instance, null);
                               }
                               finally
                               {
                                   var written = log?.ToString();
                                   if (!string.IsNullOrEmpty(written))
                                   {
                                       Console.WriteLine(written);
                                   }
                               }
                               return instance;
                           };
            }
        }

        public override FixtureOneTimeTearDownAction OneTimeTearDown
        {
            get
            {
                return (type, state) =>
                           {
                               var method = GetOneTimeMethod(type, typeof (OneTimeTearDownAttribute));
                               if (method == null)
                                   return;

                               object instance = method.IsStatic ? null : (state ?? Activator.CreateInstance(type));
                               method.Invoke(instance, null);
                           };
            }
        }

        private static MethodInfo GetOneTimeMethod(Type type, Type attributeType)
        {
            return type.GetFlattenedMethods(includeNonPublic: true)
                .FirstOrDefault(m => m.GetCustomAttributes(attributeType, true).Any());
        }
    }
}
