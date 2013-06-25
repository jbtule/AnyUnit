using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PclUnit.Constraints.Pieces;

namespace PclUnit.Constraints
{
    public static class AssertEx
    {
        public static readonly string[] ExcludeFromStack;

        static AssertEx()
        {
            var thisType = typeof (AssertEx);

            ExcludeFromStack = new[] { string.Format("at {0}.", thisType.FullName)};
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
    }
}
