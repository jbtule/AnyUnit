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
using AnyUnit.Run;

namespace AnyUnit.Run.Attributes
{
    // The signatures of the hooks the attribute base classes in this
    // namespace expose. They used to sit in the AnyUnit root beside the
    // built-in style's own attributes, which is how the root came to hold
    // engine extension points and a style at the same time (#66). Nothing
    // outside a style package or the engine names them: a test author
    // never writes a TestInvoker, they write [Test].

    /// <seealso cref="TestAttributeBase.ParameterSets"/>
    public delegate IEnumerable<ParameterSet> TestParameterSetProducer(MethodInfo method);

    /// <seealso cref="TestAttributeBase.TestInvoke"/>
    public delegate object TestInvoker(IAssertionHelper assetHelper, MethodInfo method, object target, object[] args);

    /// <seealso cref="TestFixtureAttributeBase.ParameterSets"/>
    public delegate IEnumerable<ParameterSet> FixtureParameterSetProducer(Type type);

    /// <seealso cref="TestFixtureAttributeBase.FixtureInit"/>
    public delegate object FixtureInitializer(Type type, object[] args);

    /// <seealso cref="TestFixtureDiscoveryAttributeBase.Generator"/>
    public delegate IEnumerable<Fixture> FixtureGenerator(Assembly assembly);
}
