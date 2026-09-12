using System;
using System.Collections.Generic;
using System.Reflection;

namespace AnyUnit.Run.Attributes
{
    /// <summary>
    /// Implemented by a method-level attribute that generates full rows of
    /// arguments via indirection (xUnit's ClassDataAttribute/
    /// PropertyDataAttribute reflect a class/property - a future style's
    /// own generator could do anything else) so any style's primary
    /// TestAttributeBase can recognize a DIFFERENT style's generating
    /// attribute on the same method, without a project/assembly reference
    /// to that style - only this core interface, which every style
    /// already references. See IRowInlineParameter for the literal,
    /// one-row-per-instance counterpart.
    /// </summary>
    public interface IGeneratingParameter
    {
        IEnumerable<object[]> GetData(MethodInfo method, Type[] parameterTypes);
    }
}
