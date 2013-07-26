using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pclunit_runner
{
    public static class Util
    {
        static Util()
        {
            IsMono = Type.GetType("Mono.Runtime") != null;
        }
        public static readonly bool IsMono;
    }
}
