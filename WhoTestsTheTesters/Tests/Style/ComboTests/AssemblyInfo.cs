using AnyUnit.Style.Xunit;

// Needed for the xUnit-flavored fixtures in this assembly (TestClass +
// [Theory]/[Fact]) - AnyUnit.Style.Xunit's own discovery is entirely
// assembly-attribute-driven (see XunitStyleAttribute.cs), unlike NUnit's
// [TestFixture], which DefaultDiscovery (Runner.cs) already finds on its
// own via any class-level TestFixtureAttributeBase - no assembly
// attribute needed for those fixtures in this same assembly.
[assembly: XunitStyle]
