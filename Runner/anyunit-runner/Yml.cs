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
using AnyUnit.Run;

namespace anyunit_runner
{
    public class YamlSettings
    {
        public Config Config { get; set; }
    }

    public class Satellite
    {
        public Satellite()
        {
            Tests = new List<TestMeta>();
        }

        public string Path { get; set; }
        public string Id { get; set; }
        public IList<TestMeta> Tests { get; set; }
        public bool Connected { get; set; }
    }

    public class Config
    {
        public IList<string> Assemblies { get; set; }
        public IList<Platform> Platforms { get; set; }
    }
    public class Platform
    {
        public string Id { get; set; }
        public IList<string> Assemblies { get; set; }
    }
}
