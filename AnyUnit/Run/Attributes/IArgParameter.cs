using System.Collections;
using System.Reflection;

namespace AnyUnit.Run.Attributes
{
    /// <summary>
    /// Implemented by a PARAMETER-level attribute that supplies one
    /// parameter's own set of candidate values (NUnit's ValuesAttribute/
    /// ValueSourceAttribute/RandomAttribute) so any style's primary
    /// TestAttributeBase can build full rows by taking the cross-product
    /// across every parameter's own IArgParameter values, without a
    /// project/assembly reference to whichever style defined it - only
    /// this core interface, which every style already references.
    /// </summary>
    public interface IArgParameter
    {
        IEnumerable GetData(ParameterInfo parameter);
    }
}
