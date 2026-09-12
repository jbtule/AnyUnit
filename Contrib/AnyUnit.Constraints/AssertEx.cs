// 
//  Copyright 2013 AnyUnit Contributors
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnyUnit.Constraints.Pieces;
using AnyUnit.Util;
using System.Reflection;

namespace AnyUnit.Constraints
{
    public static class AssertEx
    {
        public static readonly IList<string> ExcludeFromStack;

        static AssertEx()
        {
            var thisType = typeof (AssertEx);

            ExcludeFromStack = new List<string> { string.Format("at {0}.", thisType.FullName)};
        }

        public static void That<T>(this IAssert assert, ref T actual, IResolveConstraint expression, string message = null)
        {
            Constraint constraint = expression.Resolve();

            if (constraint.Matches(ref actual))
            {
                assert.Okay();
                return;
            }
            using (MessageWriter writer = new TextMessageWriter(message))
            {
                constraint.WriteMessageTo(writer);
                assert.Fail(writer.ToString(), ExcludeFromStack);
            }
        }

        public static void That(this IAssert assert, object actual, IResolveConstraint expression, string message = null)
        {
            Constraint constraint = expression.Resolve();

            if (constraint.Matches(actual))
            {
                assert.Okay();
                return;
            }
            using (MessageWriter writer = new TextMessageWriter(message))
            {
                constraint.WriteMessageTo(writer);
                assert.Fail(writer.ToString(), ExcludeFromStack);
            }
        }

        public static void That(this IAssert assert, ActualValueDelegate actual, IResolveConstraint expression, string message = null)
        {
            Constraint constraint = expression.Resolve();

            if (constraint.Matches(actual))
            {
                assert.Okay();
                return;
            }
            using (MessageWriter writer = new TextMessageWriter(message))
            {
                constraint.WriteMessageTo(writer);
                assert.Fail(writer.ToString(), ExcludeFromStack);
            }
        }

        /// <summary>
        /// Real NUnit's Assert.That(TestDelegate, Throws...) form - a real,
        /// separate overload (not left to fall through to the object one
        /// above) because without it, a parameterless void lambda like
        /// Assert.That(() => engine.Process(...), Throws.InstanceOf(...))
        /// has no delegate-typed parameter to bind against here, so the
        /// compiler falls back to its own natural-type inference for the
        /// object overload and picks System.Action - a different, unrelated
        /// delegate TYPE from ThrowsConstraint's own TestDelegate, which
        /// ThrowsConstraint.Matches only ever recognized via `as TestDelegate`,
        /// so it threw ArgumentException on literally every real usage of
        /// this NUnit idiom instead of ever attempting the constraint.
        /// </summary>
        public static void That(this IAssert assert, TestDelegate actual, IResolveConstraint expression, string message = null)
        {
            Constraint constraint = expression.Resolve();

            if (constraint.Matches((object)actual))
            {
                assert.Okay();
                return;
            }
            using (MessageWriter writer = new TextMessageWriter(message))
            {
                constraint.WriteMessageTo(writer);
                assert.Fail(writer.ToString(), ExcludeFromStack);
            }
        }

        /// <summary>
        /// Same as the plain-message overload above, but with real NUnit's
        /// printf-style trailing args - a separate overload (not just
        /// giving that one a params array) so an existing 3-arg call
        /// (actual, expression) still binds to the message=null default
        /// there rather than this one's non-optional message.
        /// </summary>
        public static void That(this IAssert assert, object actual, IResolveConstraint expression, string message, params object[] args)
        {
            assert.That(actual, expression, FormatMessage(message, args));
        }

        /// <summary>
        /// Asserts that a condition is true, without a constraint - real
        /// NUnit's plain Assert.That(bool) / Assert.That(bool, string).
        /// </summary>
        public static void That(this IAssert assert, bool condition, string message = null, params object[] args)
        {
            assert.True(condition, FormatMessage(message, args));
        }

        /// <summary>
        /// Real NUnit's classic-model shorthand for
        /// That(actual, Is.EqualTo(expected)) - just delegates there.
        /// </summary>
        public static void AreEqual(this IAssert assert, object expected, object actual, string message = null)
        {
            assert.That(actual, Is.EqualTo(expected), message);
        }

        private static string FormatMessage(string message, object[] args)
        {
            return (args != null && args.Length > 0) ? string.Format(message, args) : message;
        }
    }
}
