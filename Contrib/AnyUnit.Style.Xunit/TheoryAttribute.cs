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
using System.Linq;
using System.Reflection;
using AnyUnit.Run.Attributes;
using AnyUnit.Util;

namespace AnyUnit.Style.Xunit
{
    public class TheoryAttribute:FactAttribute
    {
        public override TestParameterSetProducer ParameterSets
        {
            get
            {
                return m =>
                           {
                               var data = m.GetCustomAttributes(typeof (DataAttribute), true).OfType<DataAttribute>().ToList();
                               var dataRows = data.SelectMany(d => d.GetData(m, new Type[] { }));

                               // Any other style's row attribute (e.g. NUnit's
                               // TestCaseAttribute) also implementing
                               // IRowInlineParameter - a DataAttribute (InlineData/
                               // ClassData/PropertyData) is excluded here since its
                               // richer row(s) already came from the scan above.
                               var otherRows = m.GetCustomAttributes(true)
                                   .OfType<IRowInlineParameter>()
                                   .Where(a => !(a is DataAttribute))
                                   .Select(a => a.Arguments);

                               var rows = dataRows.Concat(otherRows).ToList();
                               if (!rows.Any())
                               {
                                   return base.ParameterSets(m);
                               }

                               return rows.Select(a => new ParameterSet(a));
                           };
            }
        }
    }


}