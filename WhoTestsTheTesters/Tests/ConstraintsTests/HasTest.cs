using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit;
using AnyUnit.Constraints;

namespace ConstraintsTests
{
    [TestFixture]
    public class HasTest:AssertionHelper
    {
        [Test]
        public void All_Success()
        {
            Assert.That(Enumerable.Range(1, 9), Has.All.AtLeast(1));
        }

        [Test]
        public void All_Fail()
        {
            Assert.That(Enumerable.Range(1, 9), Has.All.AtLeast(10));
        }


        [Test]
        public void Some_Success()
        {
            Assert.That(Enumerable.Range(1, 9), Has.Some.AtLeast(5));
        }

        [Test]
        public void Some_Fail()
        {
            Assert.That(Enumerable.Range(1, 9), Has.Some.AtLeast(10));
        }

        [Test]
        public void Exactly_Success()
        {
            Assert.That(Enumerable.Range(1, 9), Has.Exactly(5).AtLeast(5));
        }

        [Test]
        public void Exactly_Fail()
        {
            Assert.That(Enumerable.Range(1, 9), Has.Exactly(5).AtLeast(1));
        }

        [Test]
        public void Member_Success()
        {
            Assert.That(Enumerable.Range(1, 9), Has.Member(5));
        }

        [Test]
        public void Member_Fail()
        {
            Assert.That(Enumerable.Range(1, 9), Has.Member(15));
        }


        [Test]
        public void Length_Success()
        {
            Assert.That(Enumerable.Range(1, 9).ToArray(), Has.Length.EqualTo(9));
        }

        [Test]
        public void Length_Fail()
        {
            Assert.That(Enumerable.Range(1, 9).ToArray(), Has.Length.EqualTo(1));
        }

        [Test]
        public void None_Success()
        {
            Assert.That(Enumerable.Range(1, 9), Has.None.AtLeast(10));
        }

        [Test]
        public void None_Fail()
        {
            Assert.That(Enumerable.Range(1, 9), Has.None.AtLeast(3));
        }


        [Test]
        public void Count_Success()
        {
            Assert.That(Enumerable.Range(1, 9).ToList(), Has.Count.EqualTo(9));
        }

        [Test]
        public void NoProperty_Success()
        {
            Assert.That(1, Has.No.Property("Day"));
        }
        [Test]
        public void NoProperty_Fail()
        {
            Assert.That(DateTime.Today, Has.No.Property("Day"));
        }

        [Test]
        public void Count_Fail()
        {
            Assert.That(Enumerable.Range(1, 9).ToList(), Has.Count.EqualTo(4));
        }


        [Test]
        public void CountExists_Success()
        {
            Assert.That(Enumerable.Range(1, 9).ToList(), Has.Count);
        }

        [Test]
        public void CountExists_Fail()
        {
            Assert.That(Enumerable.Range(1, 9).ToArray(), Has.Count);
        }
        
        [Test]
        public void Message_Success()
        {
            Assert.That(new Exception("Test"), Has.Message.EqualTo("Test"));
        }

        [Test]
        public void Message_Fail()
        {
            Assert.That(new Exception("Test2"), Has.Message.EqualTo("Test"));
        }

        [Test]
        public void InnerException_Success()
        {
            Assert.That(new Exception("Test"), Has.InnerException.Null);
        }

        [Test]
        public void InnerException_Fail()
        {
            Assert.That(new Exception("Test2", new Exception()), Has.InnerException.Null);
        }
    }
}
