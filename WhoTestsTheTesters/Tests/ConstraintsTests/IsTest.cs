using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit;
using AnyUnit.Constraints;
namespace ConstraintsTests
{
    [TestFixture]
    public class IsTest:AssertionHelper
    {

        [Test]
        public void IsEmptyString_Success()
        {
            Assert.That("", Is.Empty);
        }

        [Test]
        public void IsEmptyString_Fail()
        {
            Assert.That("Test", Is.Empty);
        }

        [Test]
        public void IsEmptyCollection_Success()
        {
            Assert.That(Enumerable.Empty<int>, Is.Empty);
        }

        [Test]
        public void IsEmptyCollection_Fail()
        {
            Assert.That(new List<int>{1}, Is.Empty);
        }

        [Test]
        public void IsAll_Success()
        {
            Assert.That(Enumerable.Range(1, 9), Is.All.AtLeast(1));
        }

        [Test]
        public void IsAll_Fail()
        {
            Assert.That(Enumerable.Range(1, 9), Is.All.AtLeast(10));
        }

        public static IEnumerable<ParameterSet> ValuesRangeSuccessBetween1And4(MethodInfo m)
        {
            return Enumerable.Range(1, 6).Select(it => new ParameterSet(it, it <=4 ? Expected._Success : Expected._Fail));
        }

        [Test(ParameterMethod = "ValuesRangeSuccessBetween1And4")]
        public void Range(int i, Expected result)
        {
            Assert.That(i,Is.InRange(1,4));
        }


        [Test]
        public void Nan_Success()
        {
            Assert.That(0d / 0d, Is.NaN);
        }

        [Test]
        public void Nan_Fail()
        {
            Assert.That(1d, Is.NaN);
        }

        [Test]
        public void StringContaining_Success()
        {
            Assert.That("Test", Is.StringContaining("e"));
        }

        [Test]
        public void StringContaining_Fail()
        {
            Assert.That("Tssst", Is.StringContaining("e"));
        }


        [Test]
        public void StringStarting_Success()
        {
            Assert.That("test", Is.StringStarting("t"));
        }


        [Test]
        public void StringStarting_Fail()
        {
            Assert.That("test", Is.StringStarting("e"));
        }


        [Test]
        public void StringEnding_Success()
        {
            Assert.That("test", Is.StringEnding("t"));
        }

        [Test]
        public void StringEnding_Fail()
        {
            Assert.That("test", Is.StringStarting("s"));
        }

        [Test]
        public void StringStartingCaseInSensitive_Success()
        {
            Assert.That("Test", Is.StringStarting("t").IgnoreCase);
        }

        [Test]
        public void StringStartingCaseSensitive_Fail()
        {
            Assert.That("Test", Is.StringStarting("t"));
        }

        [Test]
        public void StringEqual_Success()
        {
            Assert.That("Test", Is.EqualTo("Test"));
        }

        [Test]
        public void StringEqualCaseInsensitive_Success()
        {
            Assert.That("Test", Is.EqualTo("test").IgnoreCase);
        }

        [Test]
        public void StringEqual_Fail()
        {
            Assert.That("Test", Is.EqualTo("test"));
        }

        [Test]
        public void IntEqual_Success()
        {
            Assert.That(1, Is.EqualTo(1));
        }

        [Test]
        public void IntEqual_Fail()
        {
            Assert.That(1, Is.EqualTo(-1));
        }

        [Test]
        public void IsEquivalent_Success()
        {
            Assert.That(new[]{1,2,3}, Is.EquivalentTo(new []{3,2,1}));
        }

        [Test]
        public void IsEquivalent_Fail()
        {
            Assert.That(new[] { 1, -2, 3 }, Is.EquivalentTo(new[] { 3, 2, 1 }));
        }


        [Test]
        public void Unique_Success()
        {
            Assert.That(new[] { 1, 2, 3 }, Is.Unique);
        }

        [Test]
        public void Unique_Fail()
        {
            Assert.That(new[] { 1, 2, 3, 2 }, Is.Unique);
        }
    }
}
