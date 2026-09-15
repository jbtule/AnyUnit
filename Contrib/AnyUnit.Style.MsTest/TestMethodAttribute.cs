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
    /// MSTest's [TestMethod], mapped onto TestAttributeBase.
    ///
    /// Structurally this is AnyUnit.Style.Nunit's TestAttribute with
    /// MSTest's spellings - deliberately so. The try/catch/finally shape in
    /// TestInvoke in particular is copied rather than reinvented, because
    /// its exact arrangement is what makes TestCycleExceptions classify
    /// correctly: a throw from [TestInitialize] must be an Error even
    /// though a throw from the body would be a Fail, teardown must run
    /// regardless, and a teardown throw must not mask the test's own
    /// exception. TestCycleExceptions.GetResult encodes those rules; this
    /// method's job is only to hand it exceptions tagged with the right
    /// TestCycle.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TestMethodAttribute : TestAttributeBase
    {
        public override TestParameterSetProducer ParameterSets
        {
            get
            {
                return method =>
                           {
                               var list = new List<ParameterSet>();

                               // MSTest's own literal rows. Taken through
                               // IRowInlineParameter rather than by type so the
                               // handling below can be "everything else that
                               // implements it, minus ours" without a second
                               // reflection pass.
                               var ownRows = method.GetCustomAttributes(typeof(DataRowAttribute), true)
                                   .OfType<DataRowAttribute>()
                                   .ToList();
                               if (ownRows.Any())
                               {
                                   list.AddRange(ownRows.Select(a => new ParameterSet(a.Arguments)));
                               }

                               // Any OTHER style's row attribute on the same
                               // method - xUnit's [InlineData], NUnit's
                               // [TestCase] - recognized purely through the
                               // shared core interface, with no reference to
                               // those packages. DataRowAttribute is excluded
                               // since it is already covered above.
                               var otherRows = method.GetCustomAttributes(true)
                                   .OfType<IRowInlineParameter>()
                                   .Where(a => !(a is DataRowAttribute))
                                   .ToList();
                               if (otherRows.Any())
                               {
                                   list.AddRange(otherRows.Select(a => new ParameterSet(a.Arguments)));
                               }

                               // [DynamicData] and any other style's
                               // by-indirection generator (xUnit's
                               // [ClassData]/[PropertyData]). Unlike the rows
                               // above there is nothing to exclude: MSTest's own
                               // generator reaches this the same way everyone
                               // else's does, since DynamicDataAttribute
                               // implements IGeneratingParameter and nothing
                               // else.
                               var generatedRows = method.GetCustomAttributes(true)
                                   .OfType<IGeneratingParameter>()
                                   .SelectMany(g => g.GetData(method, method.GetParameters()
                                                                            .Select(p => p.ParameterType)
                                                                            .ToArray()))
                                   .ToList();
                               if (generatedRows.Any())
                               {
                                   list.AddRange(generatedRows.Select(a => new ParameterSet(a)));
                               }

                               // Per-parameter combinatorial values. MSTest has
                               // no equivalent of NUnit's [Values]/[Range], so
                               // this can only ever fire for a cross-style
                               // method - which is exactly why it is here: the
                               // interop is symmetric or it is not interop.
                               var values = method.GetParameters()
                                   .Select(p => new
                                                    {
                                                        Prop = p,
                                                        Attr = p.GetCustomAttributes(true)
                                                                .OfType<IArgParameter>()
                                                                .FirstOrDefault()
                                                    })
                                   .ToList();

                               if (values.Any() && values.All(v => v.Attr != null))
                               {
                                   var sets = values.Select(v => v.Attr.GetData(v.Prop).Cast<object>().ToList());
                                   var accum = Enumerable.Empty<IEnumerable<object>>();
                                   accum = sets.Aggregate(accum, CombineHelper);
                                   list.AddRange(accum.Select(v => new ParameterSet(v.ToArray())));
                               }

                               if (list.Any())
                               {
                                   return list;
                               }
                               return base.ParameterSets(method);
                           };
            }
        }

        // Cross-product accumulation, identical to Style.Nunit's. protected
        // so DataTestMethodAttribute (a subclass) inherits it usefully if it
        // ever needs to specialize ParameterSets.
        protected IEnumerable<IEnumerable<object>> CombineHelper(IEnumerable<IEnumerable<object>> accum,
                                                                IEnumerable<object> sequence)
        {
            var list = new List<IEnumerable<object>>();

            var first = !accum.Any();
            foreach (var item in sequence)
            {
                if (first)
                {
                    list.Add(new[] { item });
                }
                else
                {
                    list.AddRange(accum.Select(more => more.Concat(new[] { item })));
                }
            }
            return list;
        }

        public override TestInvoker TestInvoke
        {
            get
            {
                return (helper, method, target, args) =>
                           {
                               var ignore = method.GetCustomAttributes(typeof(IgnoreAttribute), true)
                                   .OfType<IgnoreAttribute>()
                                   .FirstOrDefault();

                               if (ignore != null)
                               {
                                   throw new IgnoreException(ignore.Message);
                               }

                               // `target` is null for a static test class -
                               // TestFixtureAttributeBase.FixtureInit returns
                               // null rather than constructing one - so fall
                               // back to the declaring type for the metadata
                               // and the [TestInitialize]/[TestCleanup] scan.
                               var targetType = target as Type
                                                ?? (target != null ? target.GetType() : method.DeclaringType);

                               // Built here and injected before anything else
                               // runs, so [TestInitialize] can already use it -
                               // which ported code does, routinely, to log
                               // setup steps. helper.Log is the per-test Log the
                               // engine created for this run, so
                               // TestContext.WriteLine output lands in this
                               // test's own Result, not on the console.
                               var context = new TestContext(helper.Log)
                                                 {
                                                     TestName = method.Name,
                                                     FullyQualifiedTestClassName = targetType.FullName,
                                                     CurrentTestOutcome = UnitTestOutcome.InProgress,
                                                 };
                               TestContext.Inject(target, context);

                               var initializeMethod = GetMethodForAttribute(targetType, typeof(TestInitializeAttribute));
                               var cleanupMethod = GetMethodForAttribute(targetType, typeof(TestCleanupAttribute));

                               var expected = method.GetCustomAttributes(typeof(ExpectedExceptionAttribute), true)
                                   .OfType<ExpectedExceptionAttribute>()
                                   .FirstOrDefault();

                               TestCycleExceptions te = null;
                               Func<TestCycleExceptions> exceptions = () => te ?? (te = new TestCycleExceptions());
                               try //TryCatch Setup Errors
                               {
                                   if (initializeMethod != null)
                                       initializeMethod.Invoke(target, null);
                                   try //TryCatch Test Errors
                                   {
                                       var result = InvokeBody(helper, method, target, args, expected);
                                       context.CurrentTestOutcome = UnitTestOutcome.Passed;
                                       return result;
                                   }
                                   catch (Exception ex)
                                   {
                                       // Classified for TestContext's benefit only -
                                       // TestCycleExceptions.GetResult makes the real
                                       // determination independently, from the same
                                       // exceptions. Kept simple on purpose: this is
                                       // what a [TestCleanup] reads, and the only
                                       // distinctions ported cleanup code ever
                                       // branches on are passed/failed/inconclusive.
                                       context.CurrentTestOutcome = Classify(ex);
                                       exceptions().Add(TestCycle.Test, ex);
                                   }
                               }
                               catch (Exception ex)
                               {
                                   context.CurrentTestOutcome = Classify(ex) == UnitTestOutcome.Inconclusive
                                                                    ? UnitTestOutcome.Inconclusive
                                                                    : UnitTestOutcome.Error;
                                   exceptions().Add(TestCycle.Setup, ex);
                               }
                               finally
                               {
                                   try //TryCatch Cleanup Errors
                                   {
                                       if (cleanupMethod != null)
                                           cleanupMethod.Invoke(target, null);
                                   }
                                   catch (Exception ex)
                                   {
                                       exceptions().Add(TestCycle.Teardown, ex);
                                   }

                                   //If any errors occur throw them to next level
                                   if (te != null)
                                   {
                                       throw exceptions();
                                   }
                               }
                               throw new Exception("This point should never be reached");
                           };
            }
        }

        /// <summary>
        /// Runs the body, applying [ExpectedException] if present.
        ///
        /// Unwrapping TargetInvocationException here (rather than leaving it
        /// to TestCycleExceptions.Add, which also unwraps) is not
        /// duplication: the expectation has to be checked against the
        /// exception the test method actually threw, and MethodInfo.Invoke
        /// always wraps that one.
        /// </summary>
        private object InvokeBody(IAssertionHelper helper, MethodInfo method, object target, object[] args,
                                  ExpectedExceptionAttribute expected)
        {
            if (expected == null)
            {
                return base.TestInvoke(helper, method, target, args);
            }

            Exception thrown = null;
            try
            {
                base.TestInvoke(helper, method, target, args);
            }
            catch (TargetInvocationException tie)
            {
                thrown = tie.InnerException ?? tie;
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            // An IgnoreException means the test opted out (a call to
            // Assert.Inconclusive inside the body). That is never what
            // [ExpectedException] was asking about, so it passes straight
            // through to be classified as Ignore rather than being treated
            // as "the wrong exception".
            if (thrown is IgnoreException)
                throw thrown;

            if (thrown == null)
            {
                var message = string.IsNullOrEmpty(expected.NoExceptionMessage)
                                  ? string.Format("Test method did not throw the expected exception {0}.",
                                                  expected.ExceptionType)
                                  : expected.NoExceptionMessage;
                helper.Assert.Fail(message);
                return null;
            }

            var matches = expected.AllowDerivedTypes
                              ? expected.ExceptionType.GetTypeInfo()
                                        .IsAssignableFrom(thrown.GetType().GetTypeInfo())
                              : thrown.GetType() == expected.ExceptionType;

            if (!matches)
            {
                // A Fail, not an Error: the code under test threw the wrong
                // thing, which is a failed expectation about that code, not
                // a broken harness. Matches real MSTest's own outcome, and
                // keeps the original exception (and its stack) as the inner.
                helper.Assert.Fail(new AssertionException(
                    string.Format("Test method threw exception {0}, but exception {1} was expected.",
                                  thrown.GetType(), expected.ExceptionType),
                    thrown));
            }

            // Counts as one assertion. Without this a test whose only
            // assertion IS the [ExpectedException] would come back
            // ResultKind.NoError - "threw nothing, asserted nothing" -
            // which for this attribute is plainly wrong.
            helper.Assert.Okay();
            return null;
        }

        private static UnitTestOutcome Classify(Exception ex)
        {
            var cycle = ex as TestCycleExceptions;
            if (cycle != null)
            {
                // A nested TestCycleExceptions can only reach here from an
                // inner style's TestInvoke; nothing better to say than
                // "not passed".
                return UnitTestOutcome.Failed;
            }

            if (ex is TargetInvocationException)
                ex = ex.InnerException ?? ex;

            if (ex is IgnoreException)
                return UnitTestOutcome.Inconclusive;
            if (ex is AssertionException)
                return UnitTestOutcome.Failed;
            return UnitTestOutcome.Error;
        }

        protected MethodInfo GetMethodForAttribute(Type type, Type attributeType)
        {
            return type.GetFlattenedMethods(includeNonPublic: true)
                .FirstOrDefault(m => m.GetCustomAttributes(attributeType, true).Any());
        }

        public override int GetTimeout(MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(TimeoutAttribute), true)
                .OfType<TimeoutAttribute>()
                .Select(it => (int?)it.Timeout)
                .FirstOrDefault()
                .GetValueOrDefault(-1);
        }

        /// <summary>[TestCategory] names, verbatim, and only those.</summary>
        public override IList<string> GetCategories(MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(TestCategoryAttribute), true)
                         .OfType<TestCategoryAttribute>()
                         .SelectMany(it => it.TestCategories)
                         .Where(it => !string.IsNullOrEmpty(it))
                         .ToList();
        }

        /// <summary>
        /// [Owner], [Priority] and [TestProperty] as the key/value bag they
        /// actually are, under the same keys real MSTest's TRX writer uses
        /// ("Owner", "Priority", and the property's own name).
        ///
        /// These used to be rendered into GetCategories as "Owner:jay" /
        /// "Priority:1" / "key=value" strings, because the style was built
        /// in parallel with the schema change that added Properties and
        /// could not depend on it. That stopgap kept the information but in
        /// the wrong shape: a report showed "Owner:jay" as a category chip,
        /// and the key/value rendering the schema was added for never fired
        /// for an MSTest suite.
        /// </summary>
        public override IDictionary<string, IList<string>> GetProperties(MethodInfo method)
        {
            var properties = new Dictionary<string, IList<string>>(StringComparer.Ordinal);

            foreach (var owner in method.GetCustomAttributes(typeof(OwnerAttribute), true).OfType<OwnerAttribute>())
                Add(properties, "Owner", owner.Owner);

            foreach (var priority in method.GetCustomAttributes(typeof(PriorityAttribute), true).OfType<PriorityAttribute>())
                Add(properties, "Priority", priority.Priority.ToString(System.Globalization.CultureInfo.InvariantCulture));

            foreach (var property in method.GetCustomAttributes(typeof(TestPropertyAttribute), true).OfType<TestPropertyAttribute>())
                Add(properties, property.Name, property.Value);

            return properties;
        }

        // A key can legitimately repeat ([TestProperty] is AllowMultiple),
        // hence a list per key rather than a single value.
        internal static void Add(IDictionary<string, IList<string>> properties, string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            IList<string> values;
            if (!properties.TryGetValue(key, out values))
            {
                values = new List<string>();
                properties[key] = values;
            }
            values.Add(value ?? "");
        }

        public override string GetDescription(MethodInfo method)
        {
            return method.GetCustomAttributes(typeof(DescriptionAttribute), true)
                .OfType<DescriptionAttribute>()
                .Select(it => it.Description)
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// MSTest's [DataTestMethod] - the spelling older MSTest required on a
    /// method carrying [DataRow]. Modern MSTest accepts a plain
    /// [TestMethod] for that, and so does this style; the two are the same
    /// attribute here, and the subclass exists purely so ported source
    /// saying [DataTestMethod] compiles unchanged.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class DataTestMethodAttribute : TestMethodAttribute
    {
    }
}
