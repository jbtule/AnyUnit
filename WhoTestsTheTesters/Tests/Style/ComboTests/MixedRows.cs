using AnyUnit.Style.Xunit;
using AnyUnit.Style.Nunit;

namespace ComboTests
{
    /// <summary>
    /// Covers AnyUnit.Run.Attributes.IRowInlineParameter: a [Theory] method
    /// mixing xUnit's own [InlineData] with NUnit's [TestCase] - confirms
    /// TheoryAttribute.ParameterSets unions rows from both without
    /// duplicating or dropping either.
    ///
    /// [Theory] is declared first: TestCaseAttribute is ALSO a
    /// TestAttributeBase (it doubles as NUnit's own primary attribute -
    /// see TestCaseAttribute.cs), so GetTopMostCustomAttribute&lt;TestAttributeBase&gt;
    /// (Fixture.cs's default GetHarnesses) picks whichever
    /// TestAttributeBase-derived attribute comes first in declaration
    /// order (confirmed CLR behavior, and already relied on by
    /// NunitTests/Basic.cs's own TestParams2_Partial, which mixes [Test]
    /// before [TestCase(...)] the same way) - [Theory] first makes it the
    /// primary, with TestCase's row folded in as data only.
    /// </summary>
    public class XunitPrimary : TestClass
    {
        [AnyUnit.Style.Xunit.Theory]
        [InlineData(1, Expected._Success)]
        [TestCase(2, Expected._Success)]
        [InlineData(3, Expected._Fail)]
        // The reverse direction of MsTestRows.cs: MSTest's own row
        // attribute feeding an xUnit-primary method. Fully qualified
        // because AnyUnit.Style.MsTest also exports an AssertionHelper,
        // which a `using` here would make ambiguous with NUnit's.
        [AnyUnit.Style.MsTest.DataRow(4, Expected._Fail)]
        public void InlineAndTestCase(int i, Expected result)
        {
            Assert.InRange(i, 0, 2);
        }
    }

    /// <summary>
    /// Covers AnyUnit.Run.Attributes.IRowInlineParameter, the other
    /// direction: a method carrying only NUnit's own [TestCase(...)] (no
    /// separate [Test] needed - TestCaseAttribute is already a full
    /// primary attribute in its own right, see TestCaseAttribute.cs)
    /// mixed with xUnit's [InlineData(...)] rows - confirms
    /// Nunit.TestAttribute.ParameterSets unions rows from both.
    /// </summary>
    [TestFixture]
    public class NunitPrimary : AssertionHelper
    {
        [TestCase(1, Expected._Success)]
        [InlineData(2, Expected._Success)]
        [TestCase(3, Expected._Fail)]
        // As above, MSTest's row attribute on an NUnit-primary method.
        [AnyUnit.Style.MsTest.DataRow(4, Expected._Fail)]
        public void TestCaseAndInline(int i, Expected expected)
        {
            Assert.True(i <= 2, string.Format("expected {0} to be at most 2", i));
        }
    }
}
