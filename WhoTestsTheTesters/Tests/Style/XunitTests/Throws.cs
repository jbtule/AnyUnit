using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PclUnit.Style.Xunit;

namespace XunitTests
{
    public class Throws : TestClass
    {

        private void WillThrowAction()
        {
            throw new NotSupportedException();
        }


        private void WontThrowAction()
        {
            
        }

        private object WillThrowFunc()
        {
            throw new NotSupportedException();
        }

        private object WontThrowFunc()
        {
            return new object();
        }

        [Fact]
        public void DoesNotThrowAction_Success()
        {
            Assert.DoesNotThrow(()=>WontThrowAction());
        }

        [Fact]
        public void DoesNotThrowAction_Fail()
        {
            Assert.DoesNotThrow(() => WillThrowAction());
        }

        [Fact]
        public void DoesNotThrowFunc_Success()
        {
            Assert.DoesNotThrow(() => WontThrowFunc());
        }

        [Fact]
        public void DoesNotThrowFunc_Fail()
        {
            Assert.DoesNotThrow(() => WillThrowFunc());
        }


        [Fact]
        public void ThrowNSEAction_Success()
        {
            Assert.Throws<NotSupportedException>(() => WillThrowAction());
        }

        [Fact]
        public void ThrowNSEAction_Fail()
        {
            Assert.Throws<NotSupportedException>(() => WontThrowAction());
        }

        [Fact]
        public void ThrowNSEFunc_Success()
        {
            Assert.Throws<NotSupportedException>(() => WillThrowFunc());
        }

        [Fact]
        public void ThrowNSEFunc_Fail()
        {
            Assert.Throws<NotSupportedException>(() => WontThrowFunc());
        }

        [Fact]
        public void ThrowWrongExceptAction_Fail()
        {
            Assert.Throws<NotImplementedException>(() => WillThrowAction());
        }

        [Fact]
        public void ThrowWrongExceptFunc_Fail()
        {
            Assert.Throws<NotImplementedException>(() => WillThrowFunc());
        } 
    }
}
