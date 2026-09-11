// ****************************************************************
// This is free software licensed under the NUnit license. You
// may obtain a copy of the license as well as information regarding
// copyright ownership at http://nunit.org.
// ****************************************************************

using System;

namespace AnyUnit.Style.Nunit
{
    /// <summary>
    /// OneTimeSetUpAttribute is used in a TestFixture to identify a method
    /// that is called once, before any of the fixture's tests are run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class OneTimeSetUpAttribute : Attribute
    {}
}
