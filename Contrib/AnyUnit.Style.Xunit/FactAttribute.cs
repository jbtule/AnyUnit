using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AnyUnit.Run.Attributes;

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

        // EVERY trait, not just the Category ones GetCategories keeps -
        // before this, a [Trait("Owner","jay")] or [Trait("Priority","2")]
        // was silently dropped on the floor, even though real xUnit carries
        // both (its CTRF output emits them as `extra.traits`, key -> array
        // of values). Category deliberately appears in both places: it stays
        // in Category because that's the key report formats single out, and
        // it's in here because the bag is meant to be the whole trait set.
        public override IDictionary<string, IList<string>> GetProperties(MethodInfo method)
        {
            return TraitProperties.ToProperties(method.GetCustomAttributes(typeof(TraitAttribute), true)
                                                      .OfType<TraitAttribute>());
        }

        public override string GetDescription(MethodInfo method)
        {
            return DisplayName;
        }
    }
}
