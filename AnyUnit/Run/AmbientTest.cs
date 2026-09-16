//
//  Copyright 2026 AnyUnit Contributors
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
using System.Threading;

namespace AnyUnit.Run
{
    /// <summary>
    /// The test that is running right now, for code that has no other way
    /// to reach it: a module-level F# `let` test with no `this`, or a
    /// vocabulary like FsUnit's `should` and Expecto's `Expect` that is
    /// called as a free function.
    /// </summary>
    /// <remarks>
    /// Set by the engine around every test body (see Test.RunHelper), for
    /// every style, so anything that asserts through
    /// <see cref="Current"/> counts against the running test's own helper -
    /// its assertions are real assertions, and a test that made none still
    /// reports NoError. That is exactly what <see cref="Assert.GlobalStyle"/>
    /// cannot do: it hands back a throwaway Assert and flips a process-wide
    /// flag that degrades NoError reporting for every test in the run,
    /// which is why it is obsolete and this exists.
    ///
    /// AsyncLocal, not ThreadStatic: it has to flow across an await in an
    /// async test body, whose continuation may land on another thread.
    /// The engine runs tests sequentially, so there is never more than one
    /// live at once; the value the body captured stays with it even after
    /// the engine clears the slot, which is how an async body that asserts
    /// after its first await still finds its own test.
    /// </remarks>
    public static class AmbientTest
    {
        private static readonly AsyncLocal<IAssertionHelper> _current = new AsyncLocal<IAssertionHelper>();

        /// <summary>
        /// The running test's assertion helper, or null outside a test
        /// body (module initialisation, a script, a helper called from
        /// setup code the engine has not wrapped).
        /// </summary>
        public static IAssertionHelper Current
        {
            get { return _current.Value; }
        }

        internal static void Enter(IAssertionHelper helper)
        {
            _current.Value = helper;
        }

        internal static void Exit()
        {
            _current.Value = null;
        }
    }
}
