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
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using PclUnit.Run;

namespace pclunit_runner
{
    public class PclUnitHub : Hub
    {
       

        public void Connect(string id)
        {
            Console.WriteLine("Connecting:{0,15}",id);
            PlatformResult.Clients = Clients;
        }

        public void List(string platformTotalJson)
        {
            var pm = Newtonsoft.Json.JsonConvert.DeserializeObject<Runner>(platformTotalJson);
            foreach (var test in pm.Assemblies.SelectMany(it=>it.Fixtures).SelectMany(it=>it.Tests))
            {
                PlatformResult.AddTest(test, pm.Platform);
            }
            Console.WriteLine("Receiving Test List:{0,15}", pm.Platform);
            PlatformResult.ReceivedTests(pm.Platform);
        
        }

  

        public void SendResult(string resultJson)
        {
            //  Console.WriteLine(resultJson);
            var result = JsonConvert.DeserializeObject<Result>(resultJson);


            var dict = PlatformResult.AddResult(result);


            PrintResults.PrintResult(dict);
        }
    }
}