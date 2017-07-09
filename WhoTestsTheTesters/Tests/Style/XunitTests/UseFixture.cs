using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Style.Xunit;

namespace XunitTests
{
    public class UseFixture:TestClass,IUseFixture<MyFixture>
    {
        public static Guid? Verifier { get; set; }

        public MyFixture Data;

        public void SetFixture(MyFixture data)
        {
            Data = data;
            if (!Verifier.HasValue)
            {
                Verifier = Data.Guid;
            }
        }

        [Fact]
        public void TestGuid_Success()
        {
            Assert.True(Verifier.Value == Data.Guid, "Guid Should Match");
        }

        [Fact]
        public void TestGuid_Fail()
        {
            Assert.True(Data.Guid == Guid.Empty, "Guid Should Not Match");
        }

    }

    public class MyFixture:IDisposable
    {

       

        public Guid Guid { get; protected set; }

        public MyFixture()
        {
            Guid = Guid.NewGuid();
        }

        public void Dispose()
        {
           Guid = Guid.Empty;
        }
    }
}
