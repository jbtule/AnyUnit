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
using PclUnit.Runner;

namespace pclunit_runner
{
    public class PclUnitHub : Hub
    {
       

        public void Connect(string id)
        {
            Console.WriteLine("*******************");
            Console.WriteLine(id);
            Console.WriteLine("*******************");
            PlatformResult.Clients = Clients;
        }

        public void List(string platformTotalJson)
        {
            Console.WriteLine("+++++++++++++++++++");
            var pm = Newtonsoft.Json.JsonConvert.DeserializeObject<Runner>(platformTotalJson);
            foreach (var test in pm.Assemblies.SelectMany(it=>it.Fixtures).SelectMany(it=>it.Tests))
            {
                PlatformResult.AddTest(test, pm.Platform);
            }
            Console.WriteLine(pm.Platform);
            Console.WriteLine("+++++++++++++++++++");
            PlatformResult.ReceivedTests(pm.Platform);
        
        }

  

        public void SendResult(string resultJson)
        {
            //  Console.WriteLine(resultJson);
            var result = JsonConvert.DeserializeObject<Result>(resultJson);


            var dict = PlatformResult.AddResult(result);


            if (dict.All(it => it.Value.Result != null))
            {
                Console.Write(result.Test.Fixture.Assembly.Name + ".");
                Console.Write(result.Test.Fixture.Name + ".");
                Console.WriteLine(result.Test.Name);
                foreach(var grpResult in dict.GroupBy(it => it.Value.Result.Kind))
                {
                    Console.Write("{0}:", grpResult.Key);
                    foreach (var keyValuePair in grpResult)
                    {
                        Console.Write(" ");
                        Console.Write(keyValuePair.Value.Platform);
                    }
                    Console.WriteLine();
                }
                var span= new TimeSpan();
                foreach (var r in dict.Select(it=>it.Value.Result))
                {
                    span += (r.EndTime - r.StartTime);
                }
                Console.WriteLine("avg time:{0}", new TimeSpan(span.Ticks / dict.Count));
              

                foreach (var lup in dict.ToLookup(it => it.Value.Result.Output))
                {

                    var name = string.Join(",", lup.Select(it => it.Value.Platform));
                   
                    Console.WriteLine("{0}:", name);
                    Console.WriteLine(lup.Key);
                }
               
                Console.WriteLine("===================");
            }
        }
    }
}