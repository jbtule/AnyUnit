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

using System.Collections.Generic;

namespace AnyUnit.Style.Xunit
{
    // Shared by FactAttribute.GetProperties and
    // UnlabeledFixtureAttribute.GetProperties. Its own file rather than a
    // member of TraitAttribute because TraitAttribute is forked from xUnit
    // and carries the Outercurve Foundation header - AnyUnit-specific
    // plumbing doesn't belong inside it.
    internal static class TraitProperties
    {
        // [Trait] is AllowMultiple, and repeating a key is meaningful -
        // [Trait("Browser","chrome")] and [Trait("Browser","firefox")] on
        // the same method are two values of one key, not a conflict. Hence
        // key -> list rather than key -> string.
        public static IDictionary<string, IList<string>> ToProperties(IEnumerable<TraitAttribute> traits)
        {
            var properties = new Dictionary<string, IList<string>>();
            foreach (var trait in traits)
            {
                if (trait.Name == null)
                    continue;

                IList<string> values;
                if (!properties.TryGetValue(trait.Name, out values))
                {
                    values = new List<string>();
                    properties[trait.Name] = values;
                }
                values.Add(trait.Value);
            }
            return properties;
        }
    }
}
