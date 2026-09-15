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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// MSTest's [TestClass], mapped onto TestFixtureAttributeBase.
    ///
    /// No assembly-level opt-in attribute is needed to be discovered:
    /// Runner's DefaultDiscovery already scans every type for any
    /// class-level TestFixtureAttributeBase, so [TestClass] is found the
    /// same way NUnit's [TestFixture] is. (AnyUnit.Style.Xunit needs
    /// [assembly: XunitStyle] only because xUnit's [Fact] carries no
    /// class-level marker at all.)
    ///
    /// Unlike NUnit's [TestFixture], this takes no constructor arguments:
    /// MSTest has no parameterized-fixture concept, so ParameterSets is
    /// left at the base's single empty set rather than inventing one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TestClassAttribute : TestFixtureAttributeBase
    {
        public override FixtureInitializer FixtureInit
        {
            get
            {
                return (type, args) =>
                           {
                               var ignore = type.GetAttributes<IgnoreAttribute>().FirstOrDefault();

                               if (ignore != null)
                               {
                                   // Thrown from fixture construction, which is
                                   // inside Test.RunHelper's own try - so every
                                   // test in the class reports Ignore, which is
                                   // what a class-level [Ignore] means. See
                                   // TestCycleExceptions.GetResult, which
                                   // deliberately treats an IgnoreException from
                                   // *any* cycle as a skip.
                                   throw new IgnoreException(ignore.Message);
                               }

                               return base.FixtureInit(type, args);
                           };
            }
        }

        public override FixtureOneTimeSetUpAction OneTimeSetUp
        {
            get
            {
                return type =>
                           {
                               var method = GetOneTimeMethod(type, typeof(ClassInitializeAttribute));
                               if (method == null)
                                   return null;

                               // Real MSTest requires [ClassInitialize] to be
                               // static; an instance method is accepted here
                               // anyway, constructed once and threaded on to
                               // [ClassCleanup] as `state` (see
                               // FixtureOneTimeSetUpAction's own doc, and
                               // Style.Nunit's identical handling of
                               // [OneTimeSetUp]). That instance is separate from
                               // the fresh-per-test instances every test gets.
                               object instance = method.IsStatic ? null : Activator.CreateInstance(type);
                               method.Invoke(instance, OneTimeArgs(method, type));
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
                               var method = GetOneTimeMethod(type, typeof(ClassCleanupAttribute));
                               if (method == null)
                                   return;

                               object instance = method.IsStatic
                                                     ? null
                                                     : (state ?? Activator.CreateInstance(type));
                               method.Invoke(instance, OneTimeArgs(method, type));
                           };
            }
        }

        /// <summary>
        /// Builds the argument array for a one-time method.
        ///
        /// This is the reason a TestContext type had to exist at all:
        /// MSTest's [ClassInitialize] signature *requires* a TestContext
        /// parameter, so ported source does not compile without it, and the
        /// method cannot be invoked without a value for it. There is no
        /// per-test Log at this point (a one-time method runs outside any
        /// single test's Result), so the context is built with a null ILog -
        /// TestContext.WriteLine then falls back to Console, matching what
        /// Style.Nunit's SetUpFixtureAttribute already does with one-time
        /// output.
        ///
        /// A parameterless one-time method is accepted too, since
        /// [ClassCleanup] genuinely takes none.
        /// </summary>
        internal static object[] OneTimeArgs(MethodInfo method, Type type)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0)
                return null;

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(TestContext))
            {
                return new object[]
                           {
                               new TestContext(null)
                                   {
                                       FullyQualifiedTestClassName = type.FullName,
                                       TestName = method.Name,
                                       CurrentTestOutcome = UnitTestOutcome.InProgress,
                                   }
                           };
            }

            throw new ArgumentException(string.Format(
                "{0}.{1} must take either no parameters or a single {2} parameter.",
                type.FullName, method.Name, typeof(TestContext).FullName));
        }

        internal static MethodInfo GetOneTimeMethod(Type type, Type attributeType)
        {
            return type.GetFlattenedMethods(includeNonPublic: true)
                .FirstOrDefault(m => m.GetCustomAttributes(attributeType, true).Any());
        }

        /// <summary>
        /// Class-level [TestCategory] and [Owner]. See
        /// [TestCategory] names on the class, verbatim.
        /// </summary>
        public override IList<string> GetCategories(Type type)
        {
            var categories = new List<string>();

            categories.AddRange(type.GetAttributes<TestCategoryAttribute>()
                                    .SelectMany(it => it.TestCategories)
                                    .Where(it => !string.IsNullOrEmpty(it)));

            return categories;
        }

        /// <summary>
        /// Class-level [Owner], as a property - see
        /// TestMethodAttribute.GetProperties for why it is no longer
        /// rendered into the category list.
        /// </summary>
        public override IDictionary<string, IList<string>> GetProperties(Type type)
        {
            var properties = new Dictionary<string, IList<string>>(StringComparer.Ordinal);
            foreach (var owner in type.GetAttributes<OwnerAttribute>())
                TestMethodAttribute.Add(properties, "Owner", owner.Owner);
            return properties;
        }

        public override string GetDescription(Type type)
        {
            return type.GetAttributes<DescriptionAttribute>()
                .Select(it => it.Description)
                .FirstOrDefault();
        }
    }
}
