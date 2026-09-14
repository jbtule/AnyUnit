using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnyUnit;
using AnyUnit.Run;
using AnyUnit.Run.Attributes;

namespace BasicTests
{
    public enum Expected
    {
        _Error,
        _Success,
        _Ignore,
        _Fail,
        _NoError
    }


    [TestFixture]
    public class Basic:AssertionHelper
    {       
        
        [Test]
        public void TestTrue_Success()
        {

            Log.Write("This is just a hardcoded true");
            Assert.True(true);
        }

        [Test]
        public void TestFalse_Success()
        {
            Assert.False(false);
        }

        [Test]
        public void TestFalse_Fail()
        {
            Assert.False(true, "Expected False");
        }


        [Test]
        public void TestTrue_Fail()
        {
            Assert.True(false, "Expected True");
        }

        [Test]
        public void TestFail_Fail()
        {
            Assert.Fail("Just Fail");
        }

        [Test]
        public bool TestReturn_Success()
        {
            return true;
        }


        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test(Timeout = 1000, Category = "Timeout")]
        public void TestTimeout_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay(); 
                Test.Sleep(100);
            }
        }

        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test(Timeout = 2000,Category = "Timeout")]
        public void TestTimeout2_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }
        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test(Timeout = 3000, Category = "Timeout")]
        public void TestTimeout3_Error()
        {
            var start = DateTime.Now;
            while ((DateTime.Now - start).Seconds < 30)
            {
                Assert.Okay();
                Test.Sleep(100);
            }
        }

        [Test(Timeout = 10000)]
        public void TestTimeout10_Success()
        {
            Assert.Okay();
        }

        [Test]
        public bool TestReturn_Fail()
        {
            return false;
        }
             
        [Test]
        public void TestNothing_NoError()
        {
        }

        [Test]
        public void Test_Error()
        {

            throw new NotSupportedException("This should be an error.");
        }

        [Test]
        public void Test_Ignore()
        {
            Assert.Ignore("Ignoring...");
        }

        public static IEnumerable<ParameterSet> GetParamSet(MethodInfo method)
        {
            return Enumerable.Range(1, 5).Select(it => new ParameterSet(it));
        }

        [Test(ParameterMethod = "GetParamSet")]
        public void TestParams_Success(int i)
        {
            Assert.True(i < 10, "You shouldn't seee this message");
        }

        [Test(ParameterMethod = "GetParamSet")]
        public void TestParams_Fail(int i)
        {
            Assert.True(i > 10, String.Format("expected {0} to be greater than 10",i));
        }


        public static IEnumerable<ParameterSet> GetPartialParamSet(MethodInfo method)
        {
            return new List<ParameterSet>
                       {
                           new ParameterSet(1,Expected._Fail),
                           new ParameterSet(2,Expected._Fail),
                           new ParameterSet(3,Expected._Fail),
                           new ParameterSet(4,Expected._Success),
                           new ParameterSet(5,Expected._Success),
                       };
        }
        [Test(ParameterMethod = "GetPartialParamSet")]
        public void TestParams_Partial(int i, Expected expected)
        {
            Assert.True(i > 3, String.Format("expected {0} to be greater than 3", i));
        }

        // Async variants of the cases above. These are not decoration: an
        // async method never throws out of the call itself - every exception
        // it raises, an AssertionException from a failed assert included, is
        // captured into the Task it returns. Before AnyUnit.Run's
        // AsyncTestResult existed, that Task was dropped unawaited, so
        // TestAsyncFalse_Fail below came back Success and every other case
        // here came back NoError. Confirmed directly by testing it: with the
        // AsyncTestResult.Unwrap call commented out of Test.RunHelper, this
        // whole block reports Success/Success/NoError/NoError/NoError/
        // NoError/NoError instead of what their names say.
        //
        // Every await here completes synchronously on purpose, so these run
        // identically on every platform including single-threaded
        // browser-wasm - see AsyncTestResult.Block, and
        // TestAsyncYield_Success at the end of this file for the other case.
        [Test]
        public async Task TestAsyncTrue_Success()
        {
            Log.Write("This is an async test whose awaits complete synchronously");
            await Task.FromResult(0);
            Assert.True(true);
        }

        [Test]
        public async Task TestAsyncFalse_Fail()
        {
            await Task.FromResult(0);
            Assert.False(true, "Expected False");
        }

        [Test]
        public async Task TestAsync_Error()
        {
            await Task.FromResult(0);
            throw new NotSupportedException("This should be an error.");
        }

        [Test]
        public async Task TestAsync_Ignore()
        {
            await Task.FromResult(0);
            Assert.Ignore("Ignoring...");
        }

        [Test]
        public async Task TestAsyncNothing_NoError()
        {
            await Task.FromResult(0);
        }

        [Test]
        public async Task<bool> TestAsyncReturn_Success()
        {
            await Task.FromResult(0);
            return true;
        }

        [Test]
        public async Task<bool> TestAsyncReturn_Fail()
        {
            await Task.FromResult(0);
            return false;
        }

        // The other half of the async story: an await that genuinely
        // suspends. Declared with [RequiresCapability(AsyncYield)], which
        // the ENGINE honours on every host - this test can never complete
        // under browser-wasm (its
        // continuation needs the thread to yield back to the browser's
        // event loop, which a test run never does), so it is reported
        // Ignored there instead. Confirmed on a real
        // headless-browser run: the task arrived with IsCompleted=False and
        // was still IsCompleted=False after a deliberate 2s busy-spin. On
        // every other platform this runs normally and must pass.
        [RequiresCapability(TestCapabilities.AsyncYield)]
        [Test]
        public async Task TestAsyncYield_Success()
        {
            await Task.Delay(1);
            Assert.True(true);
        }

        // [Timeout] against a test that hangs *in the await itself*, rather
        // than in a busy loop like TestTimeout_Error above. This is the one
        // genuinely new interaction async introduces: the engine now blocks
        // the test's own thread waiting on the Task. That stays
        // interruptible because the wait enforcing [Timeout] lives on the
        // caller's thread, outside the test's (see Test.Run) - so a hung
        // await is timed out by the very same machinery that already times
        // out a hung loop. Category "Timeout" so it is skipped on
        // browser-wasm along with the other three, which can't enforce a
        // timeout at all.
        [RequiresCapability(TestCapabilities.Timeouts)]
        [Test(Timeout = 1000, Category = "Timeout")]
        public async Task TestAsyncTimeout_Error()
        {
            Assert.Okay();
            await Task.Delay(30000);
        }
    }
}
