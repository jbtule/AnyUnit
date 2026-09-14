using AnyUnit.Style.MsTest;

namespace MsTestTests
{
    // [AssemblySetUp] is the one attribute with no MSTest counterpart - see
    // AssemblySetUpAttribute for why AnyUnit needs a class-level marker
    // where MSTest infers one, and why its scope is this class's namespace
    // rather than the literal assembly.
    //
    // Deliberately paired with [TestClass] on the same class, which is the
    // documented shape: the two derive from different bases, so
    // DefaultDiscovery finds one and the SetUpFixture scan finds the other.
    [TestClass]
    [AssemblySetUp]
    public class Global : AssertionHelper
    {
        internal static bool Initialized;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            Initialized = true;
            context.WriteLine("MsTestTests assembly initialize ran");
        }

        [TestMethod]
        public void AssemblyInitializeRanFirst_Success()
        {
            Assert.IsTrue(Initialized);
        }

        [AssemblyCleanup]
        public static void AssemblyCleanup()
        {
            Initialized = false;
        }
    }
}
