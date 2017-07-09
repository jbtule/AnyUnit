﻿// 
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
using System.IO;
using System.Linq;
using System.Text;

namespace AnyUnit
{
    public class AssertionException : ResultException
    {
        public readonly List<string> ExcludeFromStackTrace = new List<string>();

        public AssertionException()
        {

        }

        public AssertionException(string message)
            : base(message)
        {

        }

        public AssertionException(string message, Exception innerException)
            : base(message, innerException)
        {

        }


        public override string StackTrace
        {
            get
            {
                var sb = new StringBuilder();
                using (var reader = new StringReader(base.StackTrace))
                {
                    string line = reader.ReadLine();
                    while (line != null)
                    {
                        if (!ExcludeFromStackTrace.Any(e => line.Contains(e)))
                        {
                            sb.AppendLine(line);
                        }  
                        line = reader.ReadLine();
                    }
                }
                return sb.ToString();
            }
        }
    }


    public class IgnoreException : ResultException
    {
        public IgnoreException()
        {

        }

        public IgnoreException(string message)
            : base(message)
        {

        }

        public IgnoreException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    public class ResultException: Exception
    {
          public ResultException()
        {

        }

        public ResultException(string message)
            : base(message)
        {

        }

        public ResultException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}