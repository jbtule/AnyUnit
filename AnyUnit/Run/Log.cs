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
using System.IO;
using System.Linq;
using System.Text;

namespace AnyUnit.Run
{
    public class Log : ILog
    {
        StringBuilder _builder = new StringBuilder();

        public override string ToString()
        {
            return _builder.ToString();
        }

        public int IndentLevel { get; set; }

        public void Indent()
        {
            IndentLevel++;
        }

        public void UnIndent()
        {
            IndentLevel--;
            if (IndentLevel < 0)
                IndentLevel = 0;
        }

        private void WriteIndent()
        {
            if (IndentLevel == 0)
                return;
            _builder.Append(Enumerable.Repeat(' ', IndentLevel * 2).ToArray());
        }

        public void Write(string format, params object[] args)
        {
            string start =format;
            if(args.Any())
                start = String.Format(start, args);
            using (var reader = new StringReader(start))
            {
                string line = reader.ReadLine();
                bool first = true;
                while (line != null)
                {

                    if (!first)
                    {
                        _builder.AppendLine();
                    }
                    else
                    {
                        first = false;
                    }
                   
                    WriteIndent();
                    _builder.Append(line);
                    line = reader.ReadLine();
                }

            }
        }

        public void WriteLine(string format, params object[] args)
        {
            string start = format;
            if (args.Any())
                start = String.Format(start, args);
            using (var reader = new StringReader(start))
            {
                string line = reader.ReadLine(); 
                while (line != null)
                {
                    WriteIndent();
                    _builder.AppendLine(line);
                    line = reader.ReadLine(); 
                } 
            }
        }
    }
}