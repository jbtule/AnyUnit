using System.Collections;
using System.Collections.Generic;
using AnyUnit.Style.Xunit;
using AnyUnit.Style.Nunit;

namespace ComboTests
{
    /// <summary>
    /// Covers AnyUnit.Run.Attributes.IArgParameter: a [Theory] method with
    /// NO xUnit data attribute at all - its rows come entirely from
    /// NUnit's own [Values(...)] on each parameter, cross-producted by
    /// TheoryAttribute's per-parameter fallback (mirroring
    /// Nunit.TestAttribute's own CombineHelper).
    /// </summary>
    public class ValuesFallback : TestClass
    {
        [AnyUnit.Style.Xunit.Theory]
        public void InRange([Values(1, 2)] int i, [Values(Expected._Success)] Expected result)
        {
            Assert.InRange(i, 0, 2);
        }
    }

    /// <summary>
    /// Covers AnyUnit.Run.Attributes.IGeneratingParameter: a method
    /// carrying xUnit's own [ClassData(typeof(...))] directly - no
    /// [TestCase]/[Values]/etc. at all - confirms
    /// Nunit.TestAttribute.ParameterSets recognizes xUnit's method-level
    /// generating attribute via the shared interface.
    /// </summary>
    [TestFixture]
    public class ClassDataFallback : AssertionHelper
    {
        [Test]
        [ClassData(typeof(Rows))]
        public void TestClassData(int i, Expected expected)
        {
            Assert.True(i <= 2, string.Format("expected {0} to be at most 2", i));
        }

        public class Rows : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[] { 1, Expected._Success },
                new object[] { 2, Expected._Success },
                new object[] { 3, Expected._Fail }
            };

            public IEnumerator<object[]> GetEnumerator() { return _data.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }
    }
}
