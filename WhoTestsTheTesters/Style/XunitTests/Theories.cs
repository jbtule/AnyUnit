using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PclUnit.Style.Xunit;

namespace XunitTests
{
    public class Theories:TestClass
    {


        [Theory,
            InlineData(1, Expected._Success),
            InlineData(2, Expected._Success),
            InlineData(3, Expected._Fail)]
        public void InlineInRange(int i, Expected result)
        {
           Assert.InRange(i,0,2);
        }



        [Theory, PropertyData("PropertyData")]
        public void PropertyInRange(int i, Expected result)
        {
            Assert.InRange(i, 0, 2);
        }

        public static IEnumerable<object[]> PropertyData
        {
            get
            {
                // Or this could read from a file. :)
                return new[]
            {
                new object[] { 1, Expected._Success },
                new object[] { 2, Expected._Success  },
                new object[] { 3, Expected._Fail  }
            };
            }
        }

        [Theory, ClassData(typeof(ClassData))]
        public void ClassLessInRange(int i, Expected result)
        {
            Assert.InRange(i, 0, 2);
        }

        public class ClassData : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[] { 1, Expected._Success },
                new object[] { 2, Expected._Success  },
                new object[] { 3, Expected._Fail  }
            };

            public IEnumerator<object[]> GetEnumerator()
            { return _data.GetEnumerator(); }

            IEnumerator IEnumerable.GetEnumerator()
            { return GetEnumerator(); }
        }



    }
}
