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
using System.Linq;

namespace AnyUnit.Run
{
    public class Assert : IAssert
    {
        internal static bool _globalStyleUsed =false;

        [Obsolete("If use global style asserts you will lose the ability"
            + " to distinguish passed tests from tests that have no assertions.")]
        public static IAssert GlobalStyle
        {
            get
            {
                _globalStyleUsed = true;
                return new Assert();
            }
        }


        public static readonly string[] ExcludeFromStack;

        static Assert()
        {
            var thisType = typeof(Assert);

            ExcludeFromStack = new[] { string.Format("at {0}.", thisType.FullName)};
        }

        public int AssertCount { get; protected set; }

        
        private static AssertionException CreateException(string message = null)
        {
            var assertion = new AssertionException();
            if (message != null)
                assertion = new AssertionException(message);
            return assertion;
        }


        public void Fail(AssertionException assertion)
        {
            AssertCount++;
            foreach (var s in ExcludeFromStack)
            {
                assertion.ExcludeFromStackTrace.Add(s);
            }
            throw assertion;
        }

        public void Fail(string message = null, IEnumerable<string> excludedFromStackTrace = null )
        {
            excludedFromStackTrace = excludedFromStackTrace ?? Enumerable.Empty<string>();
            var assertion = CreateException(message);
            foreach (var s in excludedFromStackTrace)
            {
                assertion.ExcludeFromStackTrace.Add(s);
            }
            Fail(assertion);
        }



        public void Okay()
        {
            AssertCount++;
        }

        public void Ignore(string message = null)
        {
            if (message != null)
                throw new IgnoreException(message);
            throw new IgnoreException();
        }

        public void True(bool actual, string message = null)
        {
            if (actual)
            {
                Okay();
            }
            else
            {
                Fail(message);
            }
        }

        public void False(bool actual, string message = null)
        {
        
            if (actual)
            {
                Fail(message);
            }
            else
            {
                Okay();
            }
        }
    }
}