﻿// 
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

        public abstract int GetTimeout(MethodInfo method);

        public abstract IList<string> GetCategories(MethodInfo method);

        public abstract string GetDescription(MethodInfo method);
    }
}