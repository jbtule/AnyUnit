// ****************************************************************
// Copyright 2007, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

namespace AnyUnit.Constraints
{
    /// <summary>
    /// A parameterless method under test, for <c>Assert.That(code, Throws...)</c> and
    /// <c>Assert.Throws&lt;T&gt;(code)</c>.
    /// </summary>
    /// <remarks>
    /// Declared here rather than with the constraint implementations in
    /// <c>AnyUnit.Constraints.Pieces</c>: it is named in test code, and real NUnit likewise puts
    /// it in the namespace a test author already imports.
    /// </remarks>
    public delegate void TestDelegate();

    /// <summary>
    /// Produces the actual value for the delegate form of <c>Assert.That</c>.
    /// </summary>
    public delegate object ActualValueDelegate();
}
