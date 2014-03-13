// 
//  Copyright 2013 PclUnit Contributors
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
using PclUnit.Util;

namespace PclUnit.Run.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false,
                   Inherited = true)]
    public abstract class TestFixtureAttributeBase : Attribute
    {
        public virtual FixtureInitializer FixtureInit
        {
            get { return (type, args) =>  type.IsStatic() ? null :  Activator.CreateInstance(type,args); }
        }
        public virtual FixtureParameterSetProducer ParameterSets { get { return ParameterSet.GetDefaultParameterSet; } }


        public abstract IList<string> GetCategories(Type type);
        public abstract string GetDescription(Type type);

    }
}