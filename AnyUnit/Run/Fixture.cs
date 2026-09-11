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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;
using AnyUnit.Compat.NetStandardV1;

namespace AnyUnit.Run
{
    public class Fixture:FixtureMeta
    {
        public Fixture(TestFixtureAttributeBase attribute, Type type)
            : base(attribute, type)
        {
            Type = type;
            Attribute = attribute;
        }

        public Type Type { get; set; }

        public TestFixtureAttributeBase Attribute { get; set; }

        public IEnumerable<ParameterSet> ParameterSets()
        {
            return Attribute.ParameterSets(Type);
        }

        public virtual IEnumerable<TestHarness> GetHarnesses()
        {
            return Type.GetFlattenedMethods()
                .Select(m => new TestHarness(m.GetTopMostCustomAttribute<TestAttributeBase>(), m))
                .Where(th => th.Attribute != null);
        }

        // OneTimeSetUp/OneTimeTearDown bookkeeping. Both are called from
        // Test.Run() (once per actually-executed test, sequentially - the
        // Runner doesn't run tests concurrently, so no locking here).
        //
        // Known limitation: a TestFilter that excludes the last test(s) of
        // a fixture means _remainingTests never reaches zero, so
        // OneTimeTearDown won't fire for that run. Tolerable since
        // TestFilter is a lightly-used, advanced feature (mainly used by
        // the now-retired orchestrator) - not worth the extra bookkeeping
        // this would take to handle precisely.
        private bool _oneTimeSetUpAttempted;
        private object _oneTimeSetUpState;
        private Exception _oneTimeSetUpException;
        private int _remainingTests = -1;

        internal void EnsureOneTimeSetUp()
        {
            if (!_oneTimeSetUpAttempted)
            {
                _oneTimeSetUpAttempted = true;
                _remainingTests = Tests.Count;
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
                    string.Format("OneTimeSetUp failed for fixture {0}.", Type.FullName),
                    _oneTimeSetUpException);
            }
        }

        internal void NotifyTestFinished()
        {
            if (_remainingTests < 0)
                return; // EnsureOneTimeSetUp was never reached for this fixture

            _remainingTests--;
            if (_remainingTests <= 0 && Attribute.OneTimeTearDown != null)
            {
                Attribute.OneTimeTearDown(Type, _oneTimeSetUpState);
            }
        }

        // [SetUpFixture]-equivalent scopes (see NamespaceScope) that wrap
        // this fixture's tests, outermost-first (a test can be under more
        // than one nested namespace scope at once). Computed once and
        // cached - every Test sharing this Fixture has the same answer.
        private IList<NamespaceScope> _applicableNamespaceScopes;

        internal IList<NamespaceScope> ApplicableNamespaceScopes
        {
            get
            {
                if (_applicableNamespaceScopes == null)
                {
                    var ns = Type.Namespace ?? string.Empty;
                    _applicableNamespaceScopes = Assembly.NamespaceScopes
                        .Where(s => s.AppliesTo(ns))
                        .OrderBy(s => s.Namespace.Length)
                        .ToList();
                }
                return _applicableNamespaceScopes;
            }
        }
    }
}
