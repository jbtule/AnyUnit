using AnyUnit.Style.MsTest;

namespace ComboTests
{
    /// <summary>
    /// Covers AnyUnit.Run.Attributes.IRowInlineParameter and
    /// IGeneratingParameter with MSTest as the PRIMARY style: a
    /// [DataTestMethod] whose rows come from MSTest's own [DataRow], xUnit's
    /// [InlineData] and NUnit's [TestCase] at once - confirming
    /// TestMethodAttribute.ParameterSets unions rows from all three without
    /// duplicating or dropping any, and without AnyUnit.Style.MsTest
    /// referencing either other package.
    ///
    /// xUnit's and NUnit's attributes are written fully qualified rather
    /// than via a `using`, because both of those namespaces also export an
    /// AssertionHelper and a TestFixture/TestClass, and importing all three
    /// styles into one file would make those names ambiguous. The other
    /// direction (a [DataRow] feeding an xUnit [Theory] or an NUnit
    /// [TestCase] method) lives in MixedRows.cs, fully qualified there for
    /// the same reason.
    /// </summary>
    [TestClass]
    public class MsTestPrimary : AssertionHelper
    {
        [DataTestMethod]
        [DataRow(1, Expected._Success)]
        [AnyUnit.Style.Xunit.InlineData(2, Expected._Success)]
        [AnyUnit.Style.Nunit.TestCase(3, Expected._Fail)]
        [DataRow(4, Expected._Fail)]
        public void DataRowInlineDataAndTestCase(int i, Expected expected)
        {
            Assert.IsTrue(i <= 2, string.Format("expected {0} to be at most 2", i));
        }

        /// <summary>
        /// The parameter-level interface, the one shape MSTest has no
        /// attribute of its own for: NUnit's [Values] on each parameter,
        /// cross-producted by TestMethodAttribute.ParameterSets. Interop is
        /// symmetric or it is not interop.
        /// </summary>
        [TestMethod]
        public void NunitValuesOnMsTestMethod_Success(
            [AnyUnit.Style.Nunit.Values(1, 2)] int i,
            [AnyUnit.Style.Nunit.Values(10, 20)] int j)
        {
            Assert.IsTrue(i < j);
        }
    }
}
