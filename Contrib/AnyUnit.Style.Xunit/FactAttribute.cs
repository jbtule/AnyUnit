using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AnyUnit.Style.Xunit
{
    public class FactAttribute:Run.Attributes.TestAttributeBase
    {

        public virtual string DisplayName { get; set; }

        public virtual string Skip { get; set; }

        public int Timeout { get; set; }

        public override TestInvoker TestInvoke
        {
            get
            {

                return (h, m, t, a) =>
                           {

                               if (!String.IsNullOrEmpty(Skip))
                                   throw new IgnoreException(Skip);

                               return base.TestInvoke(h,m,t,a);
                           };
            }
        }


        public override int GetTimeout(MethodInfo method)
        {
            if (Timeout == 0)
                return -1;
            return Timeout;
        }

        public override IList<string> GetCategories(MethodInfo method)
        {
            return (method.GetCustomAttributes(typeof(TraitAttribute), true)
                      .OfType<TraitAttribute>()
                      .Where(trait => trait.Name == "Category")
                      .Select(trait => trait.Value)).ToList();
        }

        public override string GetDescription(MethodInfo method)
        {
            return DisplayName;
        }
    }
}
