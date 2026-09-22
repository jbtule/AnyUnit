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
using AnyUnit.Run.Attributes;

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

                               // GetFlattenedMethods(), not AllMethods(): the
                               // latter is Instance-only, which would silently
                               // exclude any type whose [Fact] methods are
                               // static - including, notably, an F# module's
                               // compiler-generated class, since F#'s own
                               // idiomatic top-level `[<Fact>] let name () = ...`
                               // compiles to a static method. Fixture.GetHarnesses()'s
                               // own default already uses GetFlattenedMethods()
                               // (Instance+Static) for the actual harness list -
                               // this just matches that same net for the
                               // fixture-qualification check itself, so a
                               // type isn't filtered out here only to have
                               // GetHarnesses() find its static [Fact] methods
                               // anyway. TestFixtureAttributeBase.FixtureInit's
                               // default already handles a static type
                               // correctly too (returns null, so TestInvoke's
                               // method.Invoke(null, args) - a static call -
                               // just works), so no further change is needed
                               // for static [Fact] methods to run correctly
                               // once discovered.
                               return types.Where(t => t.GetFlattenedMethods()
                                                       .Any(m => m.GetTopMostCustomAttribute<FactAttribute>() != null))
                                           .Select(t => new Fixture(new UnlabeledFixtureAttribute(), t));
                           };
            }
        }
    }
}
