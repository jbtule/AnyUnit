using AnyUnit.Style.Nunit;
using AnyUnit.Style.Xunit;

namespace NunitTests
{
    /// <summary>
    /// Covers AnyUnit.Run.Attributes.IRowInlineParameter, the other
    /// direction from XunitTests/MixedRows.cs: a method carrying only
    /// NUnit's own [TestCase(...)] (no separate [Test] needed -
    /// TestCaseAttribute is already a full primary attribute in its own
    /// right, see TestCaseAttribute.cs) mixed with xUnit's
    /// [InlineData(...)] rows - confirms Nunit.TestAttribute.ParameterSets
    /// unions rows from both.
    /// </summary>
    [TestFixture]
    public class MixedRows : AssertionHelper
    {
        [TestCase(1, Expected._Success)]
        [InlineData(2, Expected._Success)]
        [TestCase(3, Expected._Fail)]
        public void TestCaseAndInline(int i, Expected expected)
        {
            Assert.True(i <= 2, string.Format("expected {0} to be at most 2", i));
        }
    }
}
