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
using System.IO;
using AnyUnit.Run;

namespace anyunit_runner
{
    public static class WriteResults
    {
        public const string JsonType = "json";
        public const string NunitType = "nunit";

        public static void ToFiles(ResultsFile file, IDictionary<string, string> typesAndPaths)
        {
            foreach (var typeAndPath in typesAndPaths)
            {
                switch (typeAndPath.Key)
                {
                    default:
                        File.WriteAllText(typeAndPath.Value, file.ToListJson());
                        break;
                }
            }
        }
    }
}
