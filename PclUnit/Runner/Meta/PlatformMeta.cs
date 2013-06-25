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
using System.Linq;
using PclUnit.Util;

namespace PclUnit.Runner
{
    public class PlatformMeta : IMeta
    {
        public PlatformMeta()
        {
            Assemblies = new CallBackList<AssemblyMeta>(it => it.Platform = this);
        }

        public PlatformMeta(string name, string version= null, string arch= null, string profile= null):this()
        {
            Name = name;
            Version = version;
            arch = arch;
            profile = profile;
        }


        public string UniqueName
        {
            get { return "L:" + String.Format("{0}-{1}-{2}-{3}", Name, Version, Arch, Profile); }
        }

        public string Name { get; set; }

        public string ToListJson()
        {
            return String.Format("{{Name:\"{0}\", UniqueName:\"{1}\", Version:\"{2}\", Profile:\"{3}\", Arch:\"{4}\" Assemblies:[{5}]}}",
                                 Name, UniqueName, Version, Profile, Arch,
                                 String.Join(",", Assemblies.Select(it => it.ToListJson()).ToArray())
                );
        }

        public string ToItemJson()
        {
            return String.Format("{{ Profile:\"{3}\", Arch:\"{4}\", UniqueName:\"{1}\", Version:\"{2}\",Name:\"{0}\"}}",
                                 Name, UniqueName, Version, Profile, Arch
                );
        }

        public string Profile { get; set; }

        public string Arch { get; set; }

        public string Version { get; set; }

        public IList<AssemblyMeta> Assemblies { get; protected set; }

    }
}