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
    public class ThrowTest:AssertionHelper
    {
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


        [Test]
        public void TestAssertThrowArgument_Success()
        {
            Assert.That(() => { throw new ArgumentException("Test", "arg"); }, Throws.ArgumentException);
        }

        [Test]
        public void TestAssertThrowArgument_Fail()
        {
            Assert.That(() => { throw new NotSupportedException(); }, Throws.ArgumentException);
        }

        [Test]
        public void TestAssertThrowException_Success()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.Exception);
        }

        [Test]
        public void TestAssertThrowException_Fail()
        {
            Assert.That(() => { return String.Empty; }, Throws.Exception);
        }

        [Test]
        public void TestAssertThrowInner_Success()
        {
            Assert.That(() => { throw new Exception("lala", new NotImplementedException()); },
                Throws.InnerException.InstanceOf<NotImplementedException>());
        }
        [Test]
        public void TestAssertThrowInner_Fail()
        {
            Assert.That(() => { throw new Exception("lala"); },
                Throws.InnerException.InstanceOf<NotImplementedException>());
        }

        [Test]
        public void TestAssertThrowNothing_Success()
        {
            Assert.That(() => { return String.Empty; }, Throws.Nothing);
        }

        [Test]
        public void TestAssertThrowNothing_Fail()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.Nothing);
        }

        [Test]
        public void TestAssertThrowInvalid_Success()
        {
            Assert.That(() => { throw new InvalidOperationException(); }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestAssertThrowInvalid_Fail()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.InvalidOperationException);
        }

        [Test]
        public void TestAssertThrowTarget_Success()
        {
            Assert.That(() => { throw new TargetInvocationException("test", new NotImplementedException()); }, Throws.TargetInvocationException);
        }

        [Test]
        public void TestAssertThrowTarget_Fail()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.TargetInvocationException);
        }

        [Test]
        public void TestAssertThrowTypeOf_Success()
        {
            Assert.That(() => { throw new Exception(); }, Throws.TypeOf<Exception>());
        }

        [Test]
        public void TestAssertThrowTypeOf_Fail()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.TypeOf<Exception>());
        }

        [Test]
        public void TestAssertThrowInstanceOf_Success()
        {
            Assert.That(() => { throw new NotImplementedException(); }, Throws.InstanceOf<Exception>());
        }
    }
}
