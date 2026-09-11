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

namespace AnyUnit.Run.Attributes
{
    // Marks a class as a namespace-scoped setup fixture: its OneTimeSetUp
    // runs once before the first test anywhere in that namespace (and its
    // sub-namespaces) runs, and OneTimeTearDown once after the last one
    // finishes - regardless of where in Runner.Tests those tests actually
    // fall (see NamespaceScope). Discovered unconditionally by
    // Runner.Create(), the same way TestFixtureAttributeBase-marked
    // classes are, so no style needs an assembly-level opt-in attribute
    // for this to work.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public abstract class SetUpFixtureAttributeBase : Attribute
    {
        // Reuses the same delegate shapes TestFixtureAttributeBase's
        // OneTimeSetUp/OneTimeTearDown use - see those for what `state`
        // means. Both default to null (no hook) so a style that adds no
        // SetUpFixtureAttributeBase subclass at all just never has one
        // discovered.
        public virtual FixtureOneTimeSetUpAction OneTimeSetUp { get { return null; } }
        public virtual FixtureOneTimeTearDownAction OneTimeTearDown { get { return null; } }
    }
}
