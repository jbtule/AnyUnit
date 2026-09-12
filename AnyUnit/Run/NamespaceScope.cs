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
using AnyUnit.Run.Attributes;

namespace AnyUnit.Run
{
    /// <summary>
    /// Namespace-scoped counterpart to Fixture's per-fixture OneTimeSetUp/
    /// OneTimeTearDown bookkeeping (see Fixture.EnsureOneTimeSetUp /
    /// NotifyTestFinished - same idea, just keyed by namespace instead of
    /// Type). Deliberately does NOT rely on that namespace's tests being
    /// contiguous in Runner.Tests: `_remainingTests` is the exact count of
    /// applicable tests computed once up front (Runner.Create), and
    /// EnsureOneTimeSetUp/NotifyTestFinished are called by every one of
    /// those tests regardless of where they fall in the list or how
    /// they're interleaved with other namespaces' tests.
    /// </summary>
    public class NamespaceScope
    {
        public string Namespace { get; private set; }
        public Type Type { get; private set; }
        public SetUpFixtureAttributeBase Attribute { get; private set; }

        private bool _oneTimeSetUpAttempted;
        private object _oneTimeSetUpState;
        private Exception _oneTimeSetUpException;
        private int _remainingTests;

        public NamespaceScope(string ns, Type type, SetUpFixtureAttributeBase attribute, int testCount)
        {
            Namespace = ns ?? string.Empty;
            Type = type;
            Attribute = attribute;
            _remainingTests = testCount;
        }

        /// <summary>
        /// True if a test in `testNamespace` falls under this scope - the
        /// scope's own namespace, or any sub-namespace of it. An empty
        /// scope namespace (a [SetUpFixture] with no namespace of its own)
        /// wraps every test in the assembly.
        /// </summary>
        public bool AppliesTo(string testNamespace)
        {
            return IsUnderNamespace(testNamespace, Namespace);
        }

        internal static bool IsUnderNamespace(string testNamespace, string scopeNamespace)
        {
            testNamespace = testNamespace ?? string.Empty;
            if (string.IsNullOrEmpty(scopeNamespace))
                return true;
            return testNamespace == scopeNamespace || testNamespace.StartsWith(scopeNamespace + ".");
        }

        internal void EnsureOneTimeSetUp()
        {
            if (!_oneTimeSetUpAttempted)
            {
                _oneTimeSetUpAttempted = true;
                if (Attribute.OneTimeSetUp != null)
                {
                    try
                    {
                        _oneTimeSetUpState = Attribute.OneTimeSetUp(Type);
                    }
                    catch (Exception ex)
                    {
                        _oneTimeSetUpException = ex;
                    }
                }
            }
            if (_oneTimeSetUpException != null)
            {
                throw new Exception(
                    string.Format("OneTimeSetUp failed for SetUpFixture {0} (namespace \"{1}\").", Type.FullName, Namespace),
                    _oneTimeSetUpException);
            }
        }

        internal void NotifyTestFinished()
        {
            _remainingTests--;
            if (_remainingTests <= 0 && Attribute.OneTimeTearDown != null)
            {
                Attribute.OneTimeTearDown(Type, _oneTimeSetUpState);
            }
        }
    }
}
