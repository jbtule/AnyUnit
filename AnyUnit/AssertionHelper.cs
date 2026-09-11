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

namespace AnyUnit
{


    public interface IAssert
    {
        int AssertCount { get; }
        void Fail(string message = null, IEnumerable<string> excludedFromStackTrace = null);
        void Fail(AssertionException assertion);
        void Okay();
        void Ignore(string message = null);
        void True(bool actual, string message = null);
        void False(bool actual, string message = null);

        [Obsolete("Built in Equals Do Not Call", error:true)]
        bool Equals(object obj);

      
    }



    public interface ILog
    {
        void Indent();
        void UnIndent();
        void Write(string format, params object[] args);
        void WriteLine(string format, params object[] args);
        string ToString();
    }

    public interface IAssertionHelper
    {
        ILog Log { get; set; }
        IAssert Assert { get; set; }
    }


    public class AssertionHelper:IAssertionHelper
    {
        public ILog Log { get; set; }
        public IAssert Assert { get; set; }
    }
}