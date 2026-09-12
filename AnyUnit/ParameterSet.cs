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
using System.Reflection;
using System.Text;

namespace AnyUnit
{
    public class ParameterSet
    {

        public static IEnumerable<ParameterSet> GetDefaultParameterSet(MethodInfo method)
        {
            return new List<ParameterSet>()
                                    {
                                        new ParameterSet()
                                    };
        }

        public static IEnumerable<ParameterSet> GetDefaultParameterSet(Type method)
        {
            return new List<ParameterSet>()
                                    {
                                        new ParameterSet()
                                    };
        } 



        private readonly object[] _parameters;
        private int _retainCount = 0;


        public int Index { get; set; }

        /// <summary>
        /// Non-null means this particular row should be skipped rather than
        /// invoked - the value is the ignore reason. Set by a style's own
        /// row attribute (e.g. NUnit's TestCaseAttribute.Ignore, a
        /// per-case skip real NUnit supports that a whole-method [Ignore]
        /// can't express) when building this row's ParameterSet; Test.Run
        /// throws IgnoreException for it before invoking, same as a
        /// method-level ignore does.
        /// </summary>
        public string IgnoreReason { get; set; }

        public ParameterSet(params object[] parameters)
        {
            // Defensive, not just belt-and-suspenders: a style's row attribute
            // (e.g. NUnit's TestCaseAttribute) can end up passing null here
            // even when its own author wrote a single non-null-seeming
            // argument - [TestCase(null)]'s single null literal converts
            // directly to the params array type, so C# passes it AS the
            // array rather than wrapping it in one. TestCaseAttribute now
            // guards against that itself (a null argument list there means
            // "one argument, which is null" - a real, common test case -
            // not "zero arguments"), but Parameters/DisposeParams below
            // assume a non-null array regardless of which style (or a
            // future one) got this wrong, so fall back to empty here too.
            _parameters = parameters ?? new object[0];
        }

        public ParameterSet Retain()
        {
            _retainCount++;
            return this;
        }

        public virtual bool Disposed { get; protected set; }

        public bool  Release(){
            if (--_retainCount <= 0)
            {
                DisposeParams();
                return true;
            }
            return false;
        }

        protected virtual void DisposeParams()
        {
            foreach (var o in _parameters.OfType<IDisposable>())
            {
                Disposed = true;
                o.Dispose();
            }
        }

        public object[] Parameters
        {
            get { return _parameters; }
        }

      
    }
}
