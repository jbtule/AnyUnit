// ****************************************************************
// Copyright 2012, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using AnyUnit.Constraints.Pieces;

namespace AnyUnit.Constraints
{
    /// <summary>
    /// 
    /// </summary>
    public interface INUnitEqualityComparer
    {
        /// <summary>
        /// Compares two objects for equality within a tolerance
        /// </summary>
        /// <param name="x">The first object to compare</param>
        /// <param name="y">The second object to compare</param>
        /// <param name="tolerance">The tolerance to use in the comparison</param>
        /// <returns></returns>
        bool AreEqual(object x, object y, ref Tolerance tolerance);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INUnitEqualityComparer<T>
    {
        /// <summary>
        /// Compares two objects of a given Type for equality within a tolerance
        /// </summary>
        /// <param name="x">The first object to compare</param>
        /// <param name="y">The second object to compare</param>
        /// <param name="tolerance">The tolerance to use in the comparison</param>
        /// <returns></returns>
        bool AreEqual(T x, T y, ref Tolerance tolerance);
    }

}
