using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Style.Xunit;

namespace XunitTests
{
    public class Collection : TestClass
    {
         
        [Fact]
        public void Contains_Success()
        {
            Assert.Contains(4, Enumerable.Range(1,5));
        }

        [Fact]
        public void Contains_Fail()
        {
            Assert.Contains(4, Enumerable.Range(10, 5));

        }

        [Fact]
        public void DoesNotContain_Success()
        {
            Assert.DoesNotContain(4, Enumerable.Range(10, 5));
        }

        [Fact]
        public void DoesNotContain_Fail()
        {
            Assert.DoesNotContain(4, Enumerable.Range(1, 5));

        }

        [Fact]
        public void Empty_Success()
        {
            Assert.Empty(Enumerable.Empty<string>());

        }

        [Fact]
        public void Empty_Fail()
        {
            Assert.Empty(Enumerable.Repeat("la",5));
        }

        [Fact]
        public void Equal_Success()
        {
            Assert.Equal(Enumerable.Range(1, 5), Enumerable.Range(1, 5));
        }

        [Fact]
        public void Equal_Fail()
        {
            Assert.Equal(Enumerable.Range(1, 5), Enumerable.Range(1, 5).Reverse());
        }

        [Fact]
        public void NotEqual_Success()
        {
            Assert.NotEqual(Enumerable.Range(1, 5), Enumerable.Range(1, 5).Reverse());

        }

        [Fact]
        public void NotEqual_Fail()
        {
            Assert.NotEqual(Enumerable.Range(1, 5), Enumerable.Range(1, 5));

        }
    }
}
