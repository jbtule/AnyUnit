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
using System.Reflection;
using AnyUnit.Util;

namespace AnyUnit.Run.Attributes
{
    // Runs once before the first test of a fixture actually executes (see
    // Fixture.EnsureOneTimeSetUp). Whatever it returns is threaded through
    // to the matching FixtureOneTimeTearDownAction call as `state` - a style
    // that needs a shared instance for instance-level one-time methods (as
    // opposed to static ones) can construct it here and hand it back.
    public delegate object FixtureOneTimeSetUpAction(Type type);

    // Runs once after the last test of a fixture finishes (see
    // Fixture.NotifyTestFinished). `state` is whatever the fixture's
    // FixtureOneTimeSetUpAction returned, or null if there wasn't one/it
    // returned null.
    public delegate void FixtureOneTimeTearDownAction(Type type, object state);

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,
                   Inherited = true)]
    public abstract class TestFixtureAttributeBase : Attribute
    {
        public virtual FixtureInitializer FixtureInit
        {
            get { return (type, args) =>  type.IsStatic() ? null :  Activator.CreateInstance(type,args); }
        }
        public virtual FixtureParameterSetProducer ParameterSets { get { return ParameterSet.GetDefaultParameterSet; } }

        // Both default to no hook (null) - styles that don't have a
        // one-time-setup concept (e.g. AnyUnit's own default style,
        // AnyUnit.Style.Xunit) don't need to override either.
        public virtual FixtureOneTimeSetUpAction OneTimeSetUp { get { return null; } }
        public virtual FixtureOneTimeTearDownAction OneTimeTearDown { get { return null; } }

        // Class-level requirements, unioned with each test's own - see
        // TestAttributeBase.GetRequiredCapabilities for why this is
        // virtual rather than abstract.
        public virtual TestCapabilities GetRequiredCapabilities(Type type)
        {
            var required = TestCapabilities.None;
            foreach (var attribute in type.GetTypeInfo().GetCustomAttributes(typeof(RequiresCapabilityAttribute), true))
                required |= ((RequiresCapabilityAttribute)attribute).Required;
            return required;
        }

        public abstract IList<string> GetCategories(Type type);
        public abstract string GetDescription(Type type);

        // Arbitrary key -> values metadata for the fixture (FixtureMeta.
        // Properties). Virtual with an empty default rather than abstract
        // like GetCategories above: every existing style outside this repo
        // subclasses this, and making it abstract would break all of them
        // to add a field most styles have no source for. A style with no
        // property concept simply doesn't override it.
        public virtual IDictionary<string, IList<string>> GetProperties(Type type)
        {
            return new Dictionary<string, IList<string>>();
        }

    }
}