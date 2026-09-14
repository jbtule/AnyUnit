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

namespace AnyUnit.Run.Attributes
{


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false,
                   Inherited = true)]
    public abstract class TestAttributeBase : Attribute
    {
        public virtual TestInvoker TestInvoke
        {
            get { return (helper, method, target, args) => method.Invoke(target, args); }
        }

        public virtual TestParameterSetProducer ParameterSets 
        {
            get { return ParameterSet.GetDefaultParameterSet; } 
        }

        // Virtual with a working default, not abstract like the
        // GetCategories/GetTimeout beside it: every existing style - in
        // this repo and out of it - already compiles, and gets
        // [RequiresCapability] support for free without touching a line.
        // Making it abstract would break every third-party style for a
        // feature most of them never need to customise.
        //
        // Override it where a style's tests are not attributed methods and
        // so cannot carry the attribute (see RequiresCapabilityAttribute's
        // own remarks).
        public virtual TestCapabilities GetRequiredCapabilities(MethodInfo method)
        {
            var required = TestCapabilities.None;
            foreach (var attribute in method.GetCustomAttributes(typeof(RequiresCapabilityAttribute), true))
                required |= ((RequiresCapabilityAttribute)attribute).Required;
            return required;
        }

        public abstract int GetTimeout(MethodInfo method);

        public abstract IList<string> GetCategories(MethodInfo method);

        // Arbitrary key -> values metadata for the test (TestMeta.
        // Properties). Virtual with an empty default rather than abstract -
        // see TestFixtureAttributeBase.GetProperties for why.
        public virtual IDictionary<string, IList<string>> GetProperties(MethodInfo method)
        {
            return new Dictionary<string, IList<string>>();
        }

        public abstract string GetDescription(MethodInfo method);
    }
}