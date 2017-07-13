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
using System.Text;
using AnyUnit.Run;
using System.Reflection;
using AnyUnit.Util;

namespace AnyUnit.Style.Xunit
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class XunitStyleAttribute:Run.Attributes.TestFixtureDiscoveryAttributeBase
    {
        public override FixtureGenerator Generator
        {
            get
            {
                return a =>
                           {
                               var types = a.AllTypes();

                               return types.Where(t =>t.AllMethods()
                                                       .Any(m => m.GetTopMostCustomAttribute<FactAttribute>() != null))
                                           .Select(t => new Fixture(new UnlabeledFixtureAttribute(), t));
                           };
            }
        }
    }
}
