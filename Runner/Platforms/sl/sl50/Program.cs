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
using ManyConsole;
using SatelliteRunner.Shared;

namespace sl_runner
{
    class Program
    {
        
        static void Main(string[] args)
        {
#if x64
            if (!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif

            var commands = new ConsoleCommand[]
                               {
                                   new RunAloneCommand(),
                                   new RunSatelliteCommand(),
                               };

            // then run them.
            ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        
    }
}
