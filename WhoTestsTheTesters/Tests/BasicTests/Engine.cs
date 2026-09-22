using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnyUnit;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;
using AnyUnit.Style.Core;

// Engine paths no other self-test reached, each used here the way a
// consumer would use it (this is still a real test assembly run through
// every runner, not a unit test of the engine): a custom assembly-level
// discovery generator, a fixture whose one-time setup or Dispose throws,
// a test that hands back an IReturnedResult, a parameter row ignored per
// case, Assert.Ignore with no message, log output full of characters the
// results-file JSON has to escape, and the TestFilter the runners build
// from their include/exclude options. Every name says what the engine
// must report for it.

[assembly: TestFixtureDiscovery(
    TargetOfGenerator = typeof(BasicTests.Engine.GeneratedDiscovery),
    StaticMethodOfGenerator = "Generate")]

namespace BasicTests.Engine
{
    // [assembly: TestFixtureDiscovery] with a static generator: fixtures
    // the default [TestFixture] scan cannot see (this class carries no
    // attribute at all) can still be handed to the engine by whatever
    // rule an assembly likes. The generator gets the assembly and returns
    // Fixtures built from an existing style attribute.
    public static class GeneratedDiscovery
    {
        public static IEnumerable<Fixture> Generate(Assembly assembly)
        {
            yield return new Fixture(new TestFixtureAttribute(), typeof(FoundByGenerator));
        }
    }

    public class FoundByGenerator : AssertionHelper
    {
        [Test]
        public void Generated_Success()
        {
            Assert.True(true);
        }

        [Test]
        public void Generated_Fail()
        {
            Assert.Fail("found by the generator, and failing on purpose");
        }
    }

    // A fixture's Dispose runs after every test (Test.Run's finally); one
    // that throws is a Teardown exception, and a Teardown exception makes
    // the test an Error even though its body passed.
    [TestFixture]
    public class DisposeThrows : AssertionHelper, IDisposable
    {
        [Test]
        public void BodyPassesButDisposeThrows_Error()
        {
            Assert.True(true);
        }

        public void Dispose()
        {
            throw new InvalidOperationException("Dispose threw on purpose");
        }
    }

    // A style whose one-time setup throws: the engine records the
    // exception once (Fixture.EnsureOneTimeSetUp) and every test in the
    // fixture is an Error naming the fixture, instead of running against
    // a half-initialised one.
    public class OneTimeSetUpThrowsFixtureAttribute : TestFixtureAttribute
    {
        public override FixtureOneTimeSetUpAction OneTimeSetUp
        {
            get { return type => { throw new InvalidOperationException("OneTimeSetUp threw on purpose"); }; }
        }
    }

    [OneTimeSetUpThrowsFixture]
    public class OneTimeSetUpThrows : AssertionHelper
    {
        [Test]
        public void First_Error()
        {
            Assert.True(true);
        }

        [Test]
        public void Second_Error()
        {
            Assert.True(true);
        }
    }

    // The namespace-scoped twin of the above: a [SetUpFixture]-style
    // attribute whose one-time setup throws makes every test under its
    // namespace an Error (NamespaceScope.EnsureOneTimeSetUp), and only
    // those - this one sits in its own sub-namespace so nothing else in
    // the assembly is touched.
    public class SetUpFailsAttribute : SetUpFixtureAttributeBase
    {
        public override FixtureOneTimeSetUpAction OneTimeSetUp
        {
            get { return type => { throw new InvalidOperationException("SetUpFixture OneTimeSetUp threw on purpose"); }; }
        }
    }

    // A test may return an IReturnedResult and decide its own outcome -
    // the same shape a returned Task<T> is unwrapped to (see
    // AsyncTestResult), here handed back directly.
    [TestFixture]
    public class ReturnsResult : AssertionHelper
    {
        [Test]
        public IReturnedResult ReturnedKind_Success()
        {
            return new Result { Kind = ResultKind.Success, AssertCount = 1, Output = "decided by the returned result" };
        }

        [Test]
        public IReturnedResult ReturnedKind_Fail()
        {
            return new Result { Kind = ResultKind.Fail, AssertCount = 1, Output = "decided by the returned result" };
        }
    }

    // ParameterSet.IgnoreReason: a single row of a parametrised test
    // skipped, with a reason, while its siblings run - what NUnit's
    // per-TestCase Ignore expresses. Assert.Ignore() with no message is
    // the other ignore path, an IgnoreException with no reason at all.
    [TestFixture]
    public class Ignores : AssertionHelper
    {
        public static IEnumerable<ParameterSet> Rows(MethodInfo method)
        {
            yield return new ParameterSet("runs_Success");
            yield return new ParameterSet("skipped_Ignore") { IgnoreReason = "this row is ignored on purpose" };
        }

        [Test(ParameterMethod = "Rows")]
        public void PerRow(string name)
        {
            Assert.True(true);
        }

        [Test]
        public void NoMessage_Ignore()
        {
            Assert.Ignore();
        }
    }

    // Everything Utility.EscapeJson has a case for, in one test's log and
    // one message: the results file this run writes must still parse
    // (run-tests.sh reads it back with Python's json module before the
    // gate ever sees it, so a bad escape fails the leg right there).
    [TestFixture]
    public class JsonEscaping : AssertionHelper
    {
        private const string Awkward =
            "quote \" backslash \\ tab \t newline \n return \r backspace \b formfeed \f bell \u0001 unicode \u00e9";

        [Test]
        public void LogWithEveryEscape_Success()
        {
            Log.Write(Awkward);
            Assert.True(true);
        }

        [Test]
        public void MessageWithEveryEscape_Fail()
        {
            Assert.Fail(Awkward);
        }
    }

    // TestFilter and TestMeta.FakeTest, as the runners use them (the
    // console runner's include/exclude options build a TestFilter the
    // same way) - a plain API test, since running a filtered pass of this
    // very assembly through the gate would change what the gate sees.
    [TestFixture]
    public class Filtering : AssertionHelper
    {
        private static TestMeta Fake(string uniqueName, params string[] categories)
        {
            var test = TestMeta.FakeTest(uniqueName);
            test.Category = categories.ToList();
            return test;
        }

        [Test]
        public void NoFilterRunsEverything_Success()
        {
            Assert.True(new TestFilter().ShouldRun(Fake("T:Anything")));
        }

        [Test]
        public void IncludeByNameRunsOnlyThat_Success()
        {
            var filter = new TestFilter(new[] { "T:Wanted" }, new string[0]);
            Assert.True(filter.ShouldRun(Fake("T:Wanted")));
            Assert.False(filter.ShouldRun(Fake("T:Other")));
        }

        [Test]
        public void ExcludeByNameRunsTheRest_Success()
        {
            var filter = new TestFilter(new string[0], new[] { "T:Unwanted" });
            Assert.False(filter.ShouldRun(Fake("T:Unwanted")));
            Assert.True(filter.ShouldRun(Fake("T:Other")));
        }

        [Test]
        public void ExcludeWinsOverInclude_Success()
        {
            var filter = new TestFilter(new[] { "T:Both" }, new[] { "T:Both" });
            Assert.False(filter.ShouldRun(Fake("T:Both")));
        }

        [Test]
        public void CategoryIncludeAndExclude_Success()
        {
            var filter = new TestFilter(new[] { "C:Fast" }, new[] { "C:Slow" });
            Assert.True(filter.ShouldRun(Fake("T:A", "Fast")));
            Assert.False(filter.ShouldRun(Fake("T:B", "Slow")));
            // Both categories: every matched category must be included.
            Assert.False(filter.ShouldRun(Fake("T:C", "Fast", "Slow")));
            Assert.False(filter.ShouldRun(Fake("T:D", "Other")));
        }

        [Test]
        public void FixtureAndAssemblyNamesFilterToo_Success()
        {
            var test = Fake("T:X");
            Assert.False(new TestFilter(new string[0], new[] { test.Fixture.UniqueName }).ShouldRun(test));
            Assert.False(new TestFilter(new string[0], new[] { test.Fixture.Assembly.UniqueName }).ShouldRun(test));
        }
    }
}

namespace BasicTests.Engine.SetUpFails
{
    [SetUpFails]
    public class Scope
    {
    }

    [TestFixture]
    public class UnderFailingSetUp : AssertionHelper
    {
        [Test]
        public void ScopeSetUpThrew_Error()
        {
            Assert.True(true);
        }
    }
}
