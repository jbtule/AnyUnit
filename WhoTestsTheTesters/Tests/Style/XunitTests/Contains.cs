using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Style.Xunit;

namespace XunitTests
{
    public class Contains:TestClass
    {
        [Fact]
        public void ContainsString_Success()
        {
            Assert.Contains("org", "Georgia");
        }

        [Fact]
        public void ContainsString_Fail()
        {
            Assert.Contains( "ORG", "Georgia");
        }

        [Fact]
        public void ContainsStringIgnoreCase_Success()
        {
            Assert.Contains("ORG", "Georgia", StringComparison.CurrentCultureIgnoreCase);
        }

        [Fact]
        public void ContainsStringIgnoreCase_Fail()
        {
            Assert.Contains("borg", "Georgia", StringComparison.CurrentCultureIgnoreCase);
        }


        [Fact]
        public void DoesNotContainString_Success()
        {
            Assert.DoesNotContain( "ORG", "Georgia");
        }

        [Fact]
        public void DoesNotContainString_Fail()
        {
            Assert.DoesNotContain("org","Georgia");
        }

        [Fact]
        public void DoesNotContainStringIgnoreCase_Success()
        {
            Assert.DoesNotContain("borg", "Georgia", StringComparison.CurrentCultureIgnoreCase);
        }

        [Fact]
        public void DoesNotContainStringIgnoreCase_Fail()
        {
            Assert.DoesNotContain("ORG", "Georgia", StringComparison.CurrentCultureIgnoreCase);
        }


    }
}
