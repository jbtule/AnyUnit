using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PclUnit;
using PclUnit.Constraints;

namespace ConstraintsTests
{
    [TestFixture]
    public class Basic:AssertionHelper
    {
        [Test]
        public void TestIsTrue_Success()
        {
            Assert.That(true, Is.True);
        }

        [Test]
        public void TestIsTrue_Fail()
        {
            Assert.That(false, Is.True);
        }

        [Test]
        public void TestIsTrueWithMessage_Fail()
        {
            Assert.That(false, Is.True, "With message");

        }

        [Test]
        public void TestIsRefTrue_Success()
        {
            bool value = true;

            Assert.That(ref value, Is.True);
        }

        [Test]
        public void TestIsRefTrue_Fail()
        {
            bool value = false;

            Assert.That(ref value, Is.True);
        }


        [Test]
        public void TestIsFuncTrue_Success()
        {

            Assert.That(()=>true, Is.True);
        }

        [Test]
        public void TestIsFuncTrue_Faile()
        {
            Assert.That(() => false, Is.True);
        }


        [Test]
        public void TestIsFalse_Success()
        {
            Assert.That(false, Is.False);
        }

        [Test]
        public void TestIsFalse_Fail()
        {
            Assert.That(true, Is.False);
        }

        [Test]
        public void TestIsFalseWithMessage_Fail()
        {
            Assert.That(true, Is.False, "With message");

        }

        [Test]
        public void TestIsRefFalse_Success()
        {
            bool value = false;

            Assert.That(ref value, Is.False);
        }

        [Test]
        public void TestIsRefFalse_Fail()
        {
            bool value = true;

            Assert.That(ref value, Is.False);
        }


        [Test]
        public void TestIsFuncFalse_Success()
        {

            Assert.That(() => false, Is.False);
        }

        [Test]
        public void TestIsFuncFalse_Faile()
        {
            Assert.That(() => true, Is.False);
        }


    }
}
