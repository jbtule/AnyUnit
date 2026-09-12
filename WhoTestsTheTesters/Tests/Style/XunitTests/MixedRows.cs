using AnyUnit.Style.Xunit;
using AnyUnit.Style.Nunit;

namespace XunitTests
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
    public class MixedRows : TestClass
    {
        [AnyUnit.Style.Xunit.Theory]
        [InlineData(1, Expected._Success)]
        [TestCase(2, Expected._Success)]
        [InlineData(3, Expected._Fail)]
        public void InlineAndTestCase(int i, Expected result)
        {
            Assert.InRange(i, 0, 2);
        }
    }
}
