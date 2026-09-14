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
using System.Reflection;

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// The outcome slot TestContext.CurrentTestOutcome reports. Mirrors
    /// real MSTest's enum member names so a ported
    /// `if (TestContext.CurrentTestOutcome == UnitTestOutcome.Failed)`
    /// inside a [TestCleanup] compiles and means the same thing.
    ///
    /// AnyUnit's own ResultKind is deliberately NOT reused here: it has a
    /// distinction MSTest doesn't (NoError - a test that threw nothing but
    /// also asserted nothing), and lacks ones MSTest has (Timeout,
    /// Aborted). The mapping happens in TestMethodAttribute, in one place.
    /// </summary>
    public enum UnitTestOutcome
    {
        Failed,
        Inconclusive,
        Passed,
        InProgress,
        Error,
        Timeout,
        Aborted,
        Unknown,
        NotRunnable,
    }

    /// <summary>
    /// A minimal stand-in for MSTest's TestContext.
    ///
    /// Unlike AnyUnit.Style.Nunit, which deliberately ships no TestContext
    /// at all (see its README's "Not covered"), this one is not optional:
    /// MSTest's [ClassInitialize] signature *requires* a TestContext
    /// parameter, so a ported suite does not even compile without the type
    /// existing. That forced hand is why the type is here, and also why it
    /// stops where it does - it covers what a ported suite needs to
    /// compile and to log, not MSTest's full data-driven/deployment API.
    ///
    /// Needs no core change: the engine already injects Assert/Log into any
    /// fixture implementing IAssertionHelper (AnyUnit/Run/Test.cs), and
    /// AnyUnit.Style.MsTest.AssertionHelper is one, so TestMethodAttribute
    /// builds the context from that same injected ILog and hands it to the
    /// instance before the test body runs.
    ///
    /// Deliberately NOT covered: Properties/DataRow/DataConnection (the
    /// old data-driven-from-a-database API - AnyUnit has no equivalent and
    /// [DataRow]/[DynamicData] cover the cases people still write),
    /// TestDeploymentDir/TestRunDirectory/DeploymentDirectory and friends
    /// (AnyUnit has no deployment step at all; a ported test wanting a path
    /// should use AppContext.BaseDirectory), AddResultFile, and
    /// BeginTimer/EndTimer.
    /// </summary>
    public class TestContext
    {
        private readonly ILog _log;

        // ILog may legitimately be null: a [ClassInitialize] or
        // [AssemblyInitialize] runs outside any single test, so there is no
        // per-test Log to capture into (see AnyUnit.Run.Test.RunHelper -
        // the Log exists to be attached to one test's Result). Rather than
        // drop those writes on the floor, WriteLine falls back to Console
        // in that case, matching what Style.Nunit's SetUpFixtureAttribute
        // already decided to do with one-time-setup output.
        public TestContext(ILog log)
        {
            _log = log;
            CurrentTestOutcome = UnitTestOutcome.Unknown;
        }

        /// <summary>The bare name of the running test method.</summary>
        public string TestName { get; set; }

        /// <summary>Namespace-qualified name of the class under test.</summary>
        public string FullyQualifiedTestClassName { get; set; }

        /// <summary>
        /// Where the test stands right now. InProgress while the body runs;
        /// set to the real outcome before [TestCleanup] is invoked, which
        /// is the one moment ported code actually reads it.
        /// </summary>
        public UnitTestOutcome CurrentTestOutcome { get; set; }

        /// <summary>
        /// Writes a literal line. Note the "{0}" indirection: ILog.WriteLine
        /// is a format-string API, so passing a message containing a brace
        /// straight through (a JSON payload, a generic type name) would
        /// throw FormatException from inside logging - which would then be
        /// reported as the test's own error. The single-argument overload
        /// must never format.
        /// </summary>
        public void WriteLine(string message)
        {
            if (_log == null)
            {
                Console.WriteLine(message);
                return;
            }
            _log.WriteLine("{0}", message);
        }

        /// <summary>Writes a formatted line, as MSTest's own overload does.</summary>
        public void WriteLine(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                WriteLine(format);
                return;
            }
            if (_log == null)
            {
                Console.WriteLine(format, args);
                return;
            }
            _log.WriteLine(format, args);
        }

        /// <summary>
        /// Sets a `TestContext` property on `target` if it has one.
        ///
        /// Done by reflection rather than a plain cast to
        /// AnyUnit.Style.MsTest.AssertionHelper (which declares the
        /// property) for one specific reason: real MSTest's own convention
        /// is that a test class declares `public TestContext TestContext
        /// { get; set; }` itself and the framework property-injects it.
        /// Ported code that does exactly that *hides* the inherited
        /// property rather than overriding it, so assigning through a
        /// base-class-typed reference would set the base's slot while the
        /// test body reads its own, permanently-null one. Reflecting on the
        /// runtime type finds whichever property the class actually
        /// declares, so both shapes work.
        /// </summary>
        internal static void Inject(object target, TestContext context)
        {
            if (target == null)
                return;

            var type = target as Type ?? target.GetType();

            // Walk the hierarchy declared-only, most-derived first, instead
            // of one GetProperty with FlattenHierarchy: when a ported class
            // declares its own TestContext AND inherits ours, both exist,
            // and a single flattened lookup by name is exactly the case
            // that throws AmbiguousMatchException. Walking finds the
            // most-derived declaration - which is the one the test body's
            // own `TestContext` identifier binds to - and stops there.
            for (var current = type; current != null; current = current.GetTypeInfo().BaseType)
            {
                var property = current.GetTypeInfo().GetDeclaredProperty("TestContext");

                if (property == null)
                    continue;
                if (!property.CanWrite)
                    return;
                if (!property.PropertyType.GetTypeInfo().IsAssignableFrom(typeof(TestContext).GetTypeInfo()))
                    return;

                var setter = property.SetMethod;
                var isStatic = setter != null && setter.IsStatic;

                // `target` is a Type, not an instance, for a static test
                // class (FixtureInit returns null for those - see
                // TestFixtureAttributeBase). An instance property then has
                // nothing to be set on; a static one still does.
                if (target is Type && !isStatic)
                    return;

                property.SetValue(isStatic ? null : target, context, null);
                return;
            }
        }
    }
}
