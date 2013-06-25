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
        public void TestAssertThrow_Success()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.InstanceOf<NotImplementedException>());
        }

        [Test]
        public void TestAssertThrow_Fail()
        {
            Assert.That(() => { throw new NotSupportedException(); }, Throws.InstanceOf<NotImplementedException>());
        }
    }
}
