// ****************************************************************
// Copyright 2007, Charlie Poole
// This is free software licensed under the NUnit license. You may
// obtain a copy of the license at http://nunit.org
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Constraints.Pieces;
using AnyUnit.Constraints;

namespace AnyUnit.Style.Nunit
{
    public class AssertionHelper:AnyUnit.AssertionHelper
    {       
        

        /// <summary>
        /// Apply a constraint to an actual value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure. Works
        /// identically to Assert.That
        /// </summary>
        /// <param name="constraint">A Constraint to be applied</param>
        /// <param name="actual">The actual value to test</param>
        public void Expect(object actual, IResolveConstraint constraint)
        {
            Assert.That(actual, constraint, null);
        }

        /// <summary>
        /// Apply a constraint to an actual value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure. Works
        /// identically to Assert.That.
        /// </summary>
        /// <param name="constraint">A Constraint to be applied</param>
        /// <param name="actual">The actual value to test</param>
        /// <param name="message">The message that will be displayed on failure</param>
        public void Expect(object actual, IResolveConstraint constraint, string message)
        {
            Assert.That(actual, constraint, message);
        }

        /// <summary>
        /// Apply a constraint to an actual value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure. Works
        /// identically to Assert.That
        /// </summary>
        /// <param name="constraint">A Constraint to be applied</param>
        /// <param name="actual">The actual value to test</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public void Expect(object actual, IResolveConstraint constraint, string message, params object[] args)
        {
            Assert.That(actual, constraint, String.Format(message,args));
        }


        /// <summary>
        /// Apply a constraint to an actual value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="expr">A Constraint expression to be applied</param>
        /// <param name="del">An ActualValueDelegate returning the value to be tested</param>
        public void Expect(ActualValueDelegate del, IResolveConstraint expr)
        {
            Assert.That(del, expr.Resolve(), null);
        }

        /// <summary>
        /// Apply a constraint to an actual value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="expr">A Constraint expression to be applied</param>
        /// <param name="del">An ActualValueDelegate returning the value to be tested</param>
        /// <param name="message">The message that will be displayed on failure</param>
        public void Expect(ActualValueDelegate del, IResolveConstraint expr, string message)
        {
            Assert.That(del, expr.Resolve(), message);
        }

        /// <summary>
        /// Apply a constraint to an actual value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="del">An ActualValueDelegate returning the value to be tested</param>
        /// <param name="expr">A Constraint expression to be applied</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public void Expect(ActualValueDelegate del, IResolveConstraint expr, string message, params object[] args)
        {
            Assert.That(del, expr, String.Format(message, args));
        }

        /// <summary>
        /// Apply a constraint to a referenced value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="constraint">A Constraint to be applied</param>
        /// <param name="actual">The actual value to test</param>
        public void Expect<T>(T actual, IResolveConstraint constraint)
        {
            Assert.That(actual, constraint.Resolve(), null);
        }

        /// <summary>
        /// Apply a constraint to a referenced value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="constraint">A Constraint to be applied</param>
        /// <param name="actual">The actual value to test</param>
        /// <param name="message">The message that will be displayed on failure</param>
        public void Expect<T>(T actual, IResolveConstraint constraint, string message)
        {
            Assert.That(actual, constraint.Resolve(), message);
        }

        /// <summary>
        /// Apply a constraint to a referenced value, succeeding if the constraint
        /// is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="expression">A Constraint to be applied</param>
        /// <param name="actual">The actual value to test</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public void Expect<T>(T actual, IResolveConstraint expression, string message, params object[] args)
        {
            Assert.That(actual, expression, String.Format(message,args));
        }

        /// <summary>
        /// Asserts that a condition is true. If the condition is false the method throws
        /// an <see cref="AssertionException"/>. Works Identically to Assert.That.
        /// </summary> 
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display if the condition is false</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public void Expect(bool condition, string message, params object[] args)
        {
            Assert.That(condition, Is.True, String.Format(message,args));
        }

        /// <summary>
        /// Asserts that a condition is true. If the condition is false the method throws
        /// an <see cref="AssertionException"/>. Works Identically to Assert.That.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display if the condition is false</param>
        public void Expect(bool condition, string message)
        {
            Assert.That(condition, Is.True, message);
        }

        /// <summary>
        /// Asserts that a condition is true. If the condition is false the method throws
        /// an <see cref="AssertionException"/>. Works Identically Assert.That.
        /// </summary>
        /// <param name="condition">The evaluated condition</param>
        public void Expect(bool condition)
        {
            Assert.That(condition, Is.True, null);
        }
  

        /// <summary>
        /// Asserts that the code represented by a delegate throws an exception
        /// that satisfies the constraint provided.
        /// </summary>
        /// <param name="code">A TestDelegate to be executed</param>
        /// <param name="constraint">A ThrowsConstraint used in the test</param>
        public void Expect(TestDelegate code, IResolveConstraint constraint)
        {
            Assert.That((object)code, constraint);
        }
    }
}
