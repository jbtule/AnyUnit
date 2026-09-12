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

                               // Any other style's method-level generating attribute
                               // (e.g. a future style's own indirection, analogous to
                               // ClassData/PropertyData) also implementing
                               // IGeneratingParameter - DataAttribute is excluded
                               // here since it's already covered by the scan above.
                               var otherGeneratedRows = m.GetCustomAttributes(true)
                                   .OfType<IGeneratingParameter>()
                                   .Where(a => !(a is DataAttribute))
                                   .SelectMany(g => g.GetData(m, new Type[] { }));

                               var rows = dataRows.Concat(otherRows).Concat(otherGeneratedRows).ToList();
                               if (rows.Any())
                               {
                                   return rows.Select(a => new ParameterSet(a));
                               }

                               // No method-level rows at all - fall back to NUnit's
                               // own per-parameter combinatorial shape: if every
                               // parameter carries an IArgParameter (e.g. NUnit's
                               // Values/ValueSource/Random), cross-product each
                               // parameter's own values into full rows.
                               var argSets = m.GetParameters()
                                   .Select(p => p.GetCustomAttributes(true).OfType<IArgParameter>().FirstOrDefault())
                                   .ToList();
                               if (argSets.Count > 0 && argSets.All(a => a != null))
                               {
                                   var sets = m.GetParameters()
                                       .Zip(argSets, (p, a) => a.GetData(p).Cast<object>().ToList());
                                   var accum = Enumerable.Empty<IEnumerable<object>>();
                                   accum = sets.Aggregate(accum, CombineArgSets);
                                   return accum.Select(v => new ParameterSet(v.ToArray()));
                               }

                               return base.ParameterSets(m);
                           };
            }
        }

        // Same cross-product shape as AnyUnit.Style.Nunit.TestAttribute's own
        // CombineHelper - reimplemented here rather than shared, since the two
        // styles don't reference each other's assemblies.
        private static IEnumerable<IEnumerable<object>> CombineArgSets(IEnumerable<IEnumerable<object>> accum, IEnumerable<object> sequence)
        {
            var list = new List<IEnumerable<object>>();
            var first = !accum.Any();
            foreach (var item in sequence)
            {
                if (first)
                {
                    list.Add(new[] { item });
                }
                else
                {
                    list.AddRange(accum.Select(more => more.Concat(new[] { item })));
                }
            }
            return list;
        }
    }


}