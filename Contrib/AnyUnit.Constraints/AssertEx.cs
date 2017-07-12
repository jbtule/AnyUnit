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
    }
}
