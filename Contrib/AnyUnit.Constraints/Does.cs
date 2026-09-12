// ****************************************************************
// Copyright 2009, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using AnyUnit.Constraints.Pieces;

namespace AnyUnit.Constraints
{
    /// <summary>
    /// Helper class with properties and methods that supply
    /// constraints that operate on strings and collections.
    ///
    /// Real NUnit split these off Is (Is.StringStarting/Is.StringContaining/
    /// etc. still work, but Does.StartWith/Does.Contain read better) -
    /// ConstraintFactory (what both Is and this delegate to) already had
    /// every one of these; only this facade was missing.
    /// </summary>
    public class Does
    {
        #region Contain

        /// <summary>
        /// Returns a new CollectionContainsConstraint checking for the
        /// presence of a particular object in the collection.
        /// </summary>
        public static CollectionContainsConstraint Contain(object expected)
        {
            return new ConstraintExpression().Contains(expected);
        }

        /// <summary>
        /// Returns a new ContainsConstraint checking for a substring -
        /// a separate overload from the object one above (rather than one
        /// parameter typed object) so a string argument actually gets
        /// substring matching, not element-of-collection matching -
        /// resolved at compile time same as ConstraintFactory's own
        /// Contains(object)/Contains(string) pair.
        /// </summary>
        public static ContainsConstraint Contain(string expected)
        {
            return new ConstraintExpression().Contains(expected);
        }

        #endregion

        #region StartWith

        /// <summary>
        /// Returns a constraint that succeeds if the actual
        /// value starts with the substring supplied as an argument.
        /// </summary>
        public static StartsWithConstraint StartWith(string expected)
        {
            return new ConstraintExpression().StartsWith(expected);
        }

        #endregion

        #region EndWith

        /// <summary>
        /// Returns a constraint that succeeds if the actual
        /// value ends with the substring supplied as an argument.
        /// </summary>
        public static EndsWithConstraint EndWith(string expected)
        {
            return new ConstraintExpression().EndsWith(expected);
        }

        #endregion

        #region Not

        /// <summary>
        /// Returns a ConstraintExpression that negates any
        /// following constraint.
        /// </summary>
        public static ConstraintExpression Not
        {
            get { return new ConstraintExpression().Not; }
        }

        #endregion
    }
}
