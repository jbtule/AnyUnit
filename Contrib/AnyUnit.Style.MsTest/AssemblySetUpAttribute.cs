//
//  Copyright 2026 AnyUnit Contributors
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
using System.Diagnostics.CodeAnalysis;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// Marks the class that holds this assembly's [AssemblyInitialize] /
    /// [AssemblyCleanup] methods.
    ///
    /// This one attribute has no MSTest counterpart, and is the single
    /// place this style asks for an edit real MSTest would not. The reason
    /// is structural, not an oversight: AnyUnit's once-per-scope hook is
    /// AnyUnit.Run.Attributes.SetUpFixtureAttributeBase, which the engine
    /// discovers as a *class-level* attribute (Runner.Create), whereas
    /// MSTest marks only the methods and infers the rest. There is no
    /// class-level marker in a ported file for the engine to find, so one
    /// has to be added:
    ///
    ///     [TestClass]
    ///     [AssemblySetUp]
    ///     public class Global { [AssemblyInitialize] public static void Init(TestContext c) { ... } }
    ///
    /// Both attributes on the same class is fine and is the expected
    /// shape - they derive from different bases, so DefaultDiscovery finds
    /// the [TestClass] and the SetUpFixture scan finds this one
    /// independently.
    ///
    /// One real behavioral difference to know about: AnyUnit scopes a
    /// SetUpFixture to its own *namespace* and that namespace's
    /// sub-namespaces, not to the whole assembly. Put the class in the
    /// suite's root namespace and the two coincide, which is where a ported
    /// MSTest assembly-level class almost always already sits. In a suite
    /// with several unrelated root namespaces, it covers only its own.
    ///
    /// Deliberately NOT solved by making TestClassAttribute do double duty:
    /// an attribute has one base class, TestFixtureAttributeBase is already
    /// it, and a [TestClass] that silently became an assembly-wide setup
    /// scope whenever it happened to contain an [AssemblyInitialize] method
    /// would be a worse trade - invisible in the source, and impossible to
    /// opt out of.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AssemblySetUpAttribute : SetUpFixtureAttributeBase
    {
        // IL2067: Activator.CreateInstance on the assembly-init class.
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = Trimming.Rooted)]
        public override FixtureOneTimeSetUpAction OneTimeSetUp
        {
            get
            {
                return type =>
                           {
                               var method = TestClassAttribute.GetOneTimeMethod(
                                   type, typeof(AssemblyInitializeAttribute));
                               if (method == null)
                                   return null;

                               object instance = method.IsStatic ? null : Activator.CreateInstance(type);

                               // Mirrors Style.Nunit's SetUpFixtureAttribute: an
                               // instance-based setup fixture that implements
                               // IAssertionHelper gets a real Assert/Log, and
                               // since there is no per-test Result to attach the
                               // captured Log to, it is flushed straight to the
                               // console afterwards.
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
                                   method.Invoke(instance, TestClassAttribute.OneTimeArgs(method, type));
                               }
                               finally
                               {
                                   var written = log == null ? null : log.ToString();
                                   if (!string.IsNullOrEmpty(written))
                                   {
                                       Console.WriteLine(written);
                                   }
                               }

                               return instance;
                           };
            }
        }

        // IL2067: Activator.CreateInstance on the assembly-init class.
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = Trimming.Rooted)]
        public override FixtureOneTimeTearDownAction OneTimeTearDown
        {
            get
            {
                return (type, state) =>
                           {
                               var method = TestClassAttribute.GetOneTimeMethod(
                                   type, typeof(AssemblyCleanupAttribute));
                               if (method == null)
                                   return;

                               object instance = method.IsStatic
                                                     ? null
                                                     : (state ?? Activator.CreateInstance(type));
                               method.Invoke(instance, TestClassAttribute.OneTimeArgs(method, type));
                           };
            }
        }
    }
}
