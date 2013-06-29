using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PclUnit
{
    public class ParameterSet
    {
        private readonly object[] _parameters;
        private int _retainCount = 0;

        public ParameterSet(params object[] parameters)
        {
            _parameters = parameters;
        }

        public ParameterSet Retain()
        {
            _retainCount++;
            return this;
        }

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
                o.Dispose();
            }
        }

        public object[] Parameters
        {
            get { return _parameters; }
        }

      
    }
}
