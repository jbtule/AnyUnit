using System.Collections.Generic;
using AnyUnit.Style.MsTest;

namespace MsTestTests
{
    [TestClass]
    public class Paramed : AssertionHelper
    {
        // Each row's Expected argument ends up in the generated test name,
        // which is what the convention gate matches on - so this one method
        // covers both outcomes at once.
        [DataTestMethod]
        [DataRow(1, Expected._Success)]
        [DataRow(2, Expected._Success)]
        [DataRow(3, Expected._Fail)]
        public void DataRows_Partial(int i, Expected expected)
        {
            Assert.IsTrue(i < 3, string.Format("expected {0} to be less than 3", i));
        }

        // Modern MSTest allows [DataRow] on a plain [TestMethod]; older
        // MSTest required [DataTestMethod]. Both work here.
        [TestMethod]
        [DataRow("a", Expected._Success)]
        [DataRow("", Expected._Fail)]
        public void DataRowsOnPlainTestMethod_Partial(string value, Expected expected)
        {
            Assert.IsFalse(string.IsNullOrEmpty(value), "expected a non-empty string");
        }

        // [DataRow(null)] - a lone null literal - is the params-binding trap
        // DataRowAttribute's constructor corrects: without that correction
        // this would silently become a zero-argument row and fail to bind.
        [DataTestMethod]
        [DataRow(null)]
        public void DataRowSingleNull_Success(string value)
        {
            Assert.IsNull(value);
        }

        [DataTestMethod]
        [DynamicData(nameof(RowProperty))]
        public void DynamicDataProperty_Partial(int i, Expected expected)
        {
            Assert.IsTrue(i < 3, string.Format("expected {0} to be less than 3", i));
        }

        public static IEnumerable<object[]> RowProperty
        {
            get
            {
                yield return new object[] { 1, Expected._Success };
                yield return new object[] { 3, Expected._Fail };
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(RowMethod), DynamicDataSourceType.Method)]
        public void DynamicDataMethod_Partial(int i, Expected expected)
        {
            Assert.IsTrue(i < 3, string.Format("expected {0} to be less than 3", i));
        }

        public static IEnumerable<object[]> RowMethod()
        {
            yield return new object[] { 2, Expected._Success };
            yield return new object[] { 4, Expected._Fail };
        }

        [DataTestMethod]
        [DynamicData(nameof(ExternalRows.Rows), typeof(ExternalRows))]
        public void DynamicDataForeignType_Partial(int i, Expected expected)
        {
            Assert.IsTrue(i < 3, string.Format("expected {0} to be less than 3", i));
        }
    }

    public static class ExternalRows
    {
        public static IEnumerable<object[]> Rows
        {
            get
            {
                yield return new object[] { 0, Expected._Success };
                yield return new object[] { 5, Expected._Fail };
            }
        }
    }
}
