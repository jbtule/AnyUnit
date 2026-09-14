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

using System;

namespace AnyUnit.Style.MsTest
{
    // The plain marker/metadata attributes, all in one file on purpose.
    // Unlike AnyUnit.Style.Nunit - whose one-attribute-per-file layout
    // mirrors real NUnit's own source tree, because those files are
    // genuinely ported from it - nothing here is forked from MSTest.
    // Every one of these is a two-line declaration written from the
    // documented public shape of the attribute, so splitting them across
    // fifteen files would be pure ceremony. The attributes that carry
    // real behavior (TestClass, TestMethod, DataRow, DynamicData,
    // AssemblySetUp) each get their own file, since those are where the
    // interesting decisions live.
    //
    // None of these are *read* here: TestMethodAttribute /
    // TestClassAttribute / AssemblySetUpAttribute reflect for them. They
    // are declarations only, so a ported `using` and call site compile
    // unchanged.

    /// <summary>
    /// Marks a method that runs before each test in the class. Mapped
    /// inside TestMethodAttribute.TestInvoke, not by the engine - see
    /// that file's setup/teardown block.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TestInitializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a method that runs after each test in the class, whether the
    /// test passed or not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TestCleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the once-per-class setup method. Real MSTest requires this
    /// to be `public static void Foo(TestContext)`; AnyUnit also accepts a
    /// parameterless method and an instance method (see
    /// TestClassAttribute.OneTimeSetUp for what `state` instance threading
    /// then means), because accepting more than the framework demands
    /// costs nothing and rejecting it would only break ports.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ClassInitializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the once-per-class teardown method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ClassCleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the once-per-assembly setup method. Read by
    /// AssemblySetUpAttribute - see that file for why the containing class
    /// needs an extra [AssemblySetUp] that real MSTest does not.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AssemblyInitializeAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks the once-per-assembly teardown method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AssemblyCleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// Skips a test method, or every test in a class. Surfaces as
    /// AnyUnit.IgnoreException, so the result is Ignore, not Fail/Error.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class IgnoreAttribute : Attribute
    {
        public IgnoreAttribute()
        {
            Message = string.Empty;
        }

        public IgnoreAttribute(string message)
        {
            Message = message;
        }

        /// <summary>The reason the test is skipped.</summary>
        public string Message { get; private set; }
    }

    /// <summary>
    /// Fails a test that runs longer than Timeout milliseconds. Maps
    /// straight onto TestAttributeBase.GetTimeout, which the core already
    /// enforces (Test.Run's WaitHandle.WaitAll).
    ///
    /// Real MSTest also accepts a TestTimeout enum value
    /// ([Timeout(TestTimeout.Infinite)]); that overload is deliberately not
    /// here, because Infinite is already what AnyUnit does with no
    /// [Timeout] at all, so the only thing it could express is the default.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class TimeoutAttribute : Attribute
    {
        public TimeoutAttribute(int timeout)
        {
            Timeout = timeout;
        }

        public int Timeout { get; private set; }
    }

    /// <summary>
    /// One or more categories for a test method or class. Real MSTest
    /// takes params string[]; both shapes are supported.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class TestCategoryAttribute : Attribute
    {
        public TestCategoryAttribute(params string[] testCategories)
        {
            TestCategories = testCategories ?? new string[0];
        }

        public string[] TestCategories { get; private set; }
    }

    /// <summary>
    /// The person responsible for the test. Surfaced as a category of the
    /// form "Owner:name" - see TestMethodAttribute.GetCategories for why
    /// that shape, and what changes once the schema carries real key/value
    /// properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class OwnerAttribute : Attribute
    {
        public OwnerAttribute(string owner)
        {
            Owner = owner;
        }

        public string Owner { get; private set; }
    }

    /// <summary>
    /// A numeric priority. Surfaced as a category of the form
    /// "Priority:1" - same reasoning as OwnerAttribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class PriorityAttribute : Attribute
    {
        public PriorityAttribute(int priority)
        {
            Priority = priority;
        }

        public int Priority { get; private set; }
    }

    /// <summary>
    /// An arbitrary key/value pair attached to a test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TestPropertyAttribute : Attribute
    {
        public TestPropertyAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }

        public string Value { get; private set; }
    }

    /// <summary>
    /// Descriptive text for a test or class. Maps onto
    /// TestAttributeBase.GetDescription / TestFixtureAttributeBase.GetDescription.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class DescriptionAttribute : Attribute
    {
        public DescriptionAttribute(string description)
        {
            Description = description;
        }

        public string Description { get; private set; }
    }

    /// <summary>
    /// Legacy MSTest exception assertion: the test passes only if it
    /// throws the named exception type. Deprecated in modern MSTest in
    /// favour of Assert.ThrowsException, but extremely common in exactly
    /// the old suites this style exists to receive, so it is supported.
    /// Applied by TestMethodAttribute.TestInvoke.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExpectedExceptionAttribute : Attribute
    {
        public ExpectedExceptionAttribute(Type exceptionType)
            : this(exceptionType, string.Empty)
        {
        }

        public ExpectedExceptionAttribute(Type exceptionType, string noExceptionMessage)
        {
            ExceptionType = exceptionType;
            NoExceptionMessage = noExceptionMessage;
        }

        public Type ExceptionType { get; private set; }

        public string NoExceptionMessage { get; private set; }

        /// <summary>
        /// When false (real MSTest's own default), a *subclass* of
        /// ExceptionType does NOT satisfy the expectation - the match must
        /// be exact. This trips people up often enough to be worth stating:
        /// [ExpectedException(typeof(Exception))] does not catch an
        /// ArgumentNullException.
        /// </summary>
        public bool AllowDerivedTypes { get; set; }
    }
}
