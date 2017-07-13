﻿// ****************************************************************
// Copyright 2009, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using AnyUnit.Util;

namespace AnyUnit.Constraints.Pieces
{
    /// <summary>
    /// NUnitComparer encapsulates NUnit's default behavior
    /// in comparing two objects.
    /// </summary>
    public class NUnitComparer : IComparer
    {
        /// <summary>
        /// Returns the default NUnitComparer.
        /// </summary>
        public static NUnitComparer Default
        {
            get { return new NUnitComparer(); }
        }

        /// <summary>
        /// Compares two objects
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return +1;

            if (Numerics.IsNumericType(x) && Numerics.IsNumericType(y))
                return Numerics.Compare(x, y);

            if (x is IComparable)
                return ((IComparable)x).CompareTo(y);

            if (y is IComparable)
                return -((IComparable)y).CompareTo(x);

            Type xType = x.GetType();
            Type yType = y.GetType();

            MethodInfo method = xType.Method("CompareTo", new Type[] { yType });
            if (method != null)
                return (int)method.Invoke(x, new object[] { y });

            method = yType.Method("CompareTo", new Type[] { xType });
            if (method != null)
                return -(int)method.Invoke(y, new object[] { x });

            throw new ArgumentException("Neither value implements IComparable or IComparable<T>");
        }
    }

    /// <summary>
    /// Generic version of NUnitComparer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NUnitComparer<T> : IComparer<T>
    {
        /// <summary>
        /// Compare two objects of the same type
        /// </summary>
        public int Compare(T x, T y)
        {
            if (x == null)
                return y == null ? 0 : -1;
            else if (y == null)
                return +1;

            if (Numerics.IsNumericType(x) && Numerics.IsNumericType(y))
                return Numerics.Compare(x, y);

            if (x is IComparable<T>)
                return ((IComparable<T>)x).CompareTo(y);

            if (x is IComparable)
                return ((IComparable)x).CompareTo(y);

            throw new ArgumentException("Neither value implements IComparable or IComparable<T>");
        }
    }
}
