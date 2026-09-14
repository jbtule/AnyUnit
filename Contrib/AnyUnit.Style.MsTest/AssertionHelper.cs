//
//  Copyright 2013 AnyUnit Contributors
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

namespace AnyUnit.Style.MsTest
{
    /// <summary>
    /// Base class a ported [TestClass] must derive from. Same porting tax
    /// AnyUnit.Style.Nunit's AssertionHelper and AnyUnit.Style.Xunit's
    /// TestClass already charge, and for the same reason: AnyUnit's
    /// `Assert` is an *instance* scoped to one test's run, not a static
    /// class, so that a test which asserted nothing can be told apart from
    /// one that asserted and passed (ResultKind.NoError vs Success - see
    /// AnyUnit's own README). Deriving this is what makes the bare
    /// identifier `Assert` inside a test method resolve, unchanged, to that
    /// instance.
    ///
    /// Everything MSTest exposes as a *static class* has to be reachable by
    /// its bare simple name to keep ported call sites compiling, and that
    /// dictates the shape of this class:
    ///
    ///  - `Assert.AreEqual(...)` works because `Assert` is the inherited
    ///    IAssert property and the MSTest vocabulary is defined as
    ///    extension methods on IAssert (see AssertEx.cs) - extension
    ///    *methods* do resolve off an explicit receiver like `Assert`.
    ///
    ///  - `StringAssert.Contains(...)` and `CollectionAssert.AreEqual(...)`
    ///    have no such receiver: they are bare simple names. A bare simple
    ///    name is resolved by simple-name lookup, which considers declared
    ///    and inherited members only and never triggers extension-member
    ///    lookup - the same rule that forces `this.Select(...)` rather than
    ///    `Select(...)` inside a type that implements IEnumerable&lt;T&gt;.
    ///    So they are *declared instance properties* here.
    ///
    /// C# 14 extension properties were considered for those two and
    /// rejected: they would not change the rule above, so the ported call
    /// site would have had to become `this.CollectionAssert.AreEqual(...)`,
    /// which is exactly the edit this class exists to avoid. (Separately,
    /// this project sets no LangVersion and netstandard2.0 defaults to
    /// C# 7.3, so they would have needed an opt-in anyway.) Declaring them
    /// costs nothing, because a ported class has to gain this base type
    /// regardless - TestContext needs it too.
    ///
    /// Both properties construct on each access rather than caching in a
    /// field. `Assert` is property-injected by the engine *after* the
    /// instance is constructed (AnyUnit/Run/Test.cs), so anything captured
    /// in a constructor or a readonly field would capture null.
    /// </summary>
    public class AssertionHelper : AnyUnit.AssertionHelper
    {
        /// <summary>
        /// Stand-in for MSTest's static StringAssert class.
        /// </summary>
        public StringAssertions StringAssert
        {
            get { return new StringAssertions(Assert); }
        }

        /// <summary>
        /// Stand-in for MSTest's static CollectionAssert class.
        /// </summary>
        public CollectionAssertions CollectionAssert
        {
            get { return new CollectionAssertions(Assert); }
        }

        /// <summary>
        /// Set by TestMethodAttribute.TestInvoke before each test body
        /// runs, and by TestClassAttribute.OneTimeSetUp for
        /// [ClassInitialize]. A ported class that declares its own
        /// `public TestContext TestContext { get; set; }` (MSTest's own
        /// property-injection convention) hides this one and gets injected
        /// instead - see TestContext.Inject.
        /// </summary>
        public TestContext TestContext { get; set; }
    }
}
