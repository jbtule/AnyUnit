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

        public ParameterSet(params object[] parameters)
        {
            _parameters = parameters;
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
