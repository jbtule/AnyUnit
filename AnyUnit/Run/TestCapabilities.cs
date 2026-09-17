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
using AnyUnit.Util;

namespace AnyUnit.Run
{
    /// <summary>
    /// Runtime facilities a test may need in order to be meaningful, and
    /// which not every platform AnyUnit runs on actually has.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT categories. A category is the suite author's own
    /// taxonomy ("Slow", "Database", "Timeout"); whether a platform can
    /// run a test at all is a property of the platform. Those were
    /// conflated before this existed: the browser-wasm host excluded the
    /// category named "Timeout" outright, so any consumer who happened to
    /// label tests "Timeout" for their own unrelated reasons had them
    /// silently skipped there. Nothing announced it, and nothing let them
    /// opt out.
    ///
    /// [Flags] rather than a single value so a test can need more than one,
    /// and so this can grow without breaking anyone - a platform that later
    /// gains (or loses) a facility only changes what Available reports.
    /// </remarks>
    [Flags]
    public enum TestCapabilities
    {
        None = 0,

        /// <summary>
        /// A continuation queued by an await can actually run. An async test
        /// that genuinely suspends - awaits something not already complete -
        /// needs this; one whose awaits all complete synchronously does not,
        /// which is the overwhelmingly common case.
        /// </summary>
        /// <remarks>
        /// Absent under a single-threaded runtime (Mono on browser-wasm):
        /// the continuation only runs once the thread yields back to the
        /// browser's event loop, and a synchronous test run never does.
        /// Confirmed on a real headless-browser run - busy-spinning on
        /// IsCompleted does not pump the loop either, so there is nothing
        /// the runner can do about it. See AsyncTestResult.
        /// </remarks>
        AsyncYield = 1,

        /// <summary>
        /// Per-test [Timeout] is actually enforced, i.e. a test that hangs
        /// gets killed rather than hanging the run.
        /// </summary>
        /// <remarks>
        /// Absent under a single-threaded runtime for the same underlying
        /// reason: enforcement needs the body on one thread and the clock
        /// on another (see Test.Run), and there is only one. A test written
        /// to prove a timeout fires cannot prove anything there.
        /// </remarks>
        Timeouts = 2,

        /// <summary>
        /// More than one thread exists, so a test may block waiting for
        /// work that runs on another one: <c>Task.Run(...).Wait()</c>,
        /// <c>.Result</c> on something that genuinely runs elsewhere, F#'s
        /// <c>Async.RunSynchronously</c> under a synchronization context.
        /// </summary>
        /// <remarks>
        /// Absent under a single-threaded runtime (Mono on browser-wasm),
        /// where such a wait can never be satisfied: the work it waits for
        /// needs the very thread that is blocked. The engine cannot see a
        /// wait inside a test body the way it sees a returned Task (see
        /// <see cref="AsyncYield"/>), and timeouts cannot rescue it either,
        /// so an undeclared one hangs the run outright - found on a real
        /// F# port, where <c>async { return x } |> Async.RunSynchronously</c>
        /// hung the browser host: FSharp.Core hands the workflow to the
        /// thread pool when a SynchronizationContext is present, and
        /// Blazor has one. Declared, it is reported Ignored there instead.
        /// </remarks>
        Threads = 4,
    }

    /// <summary>
    /// What the platform this process is running on actually provides.
    /// </summary>
    public static class PlatformCapabilities
    {
        /// <summary>
        /// Capabilities available here. Computed once, from the same
        /// single-threaded-runtime probe Test.Run and AsyncTestResult
        /// already branch on, so the three cannot disagree about what this
        /// platform can do.
        /// </summary>
        public static readonly TestCapabilities Available =
            Utility.IsSingleThreadedRuntime
                ? TestCapabilities.None
                : TestCapabilities.AsyncYield | TestCapabilities.Timeouts | TestCapabilities.Threads;

        /// <summary>
        /// The subset of <paramref name="required"/> this platform does not
        /// provide - <see cref="TestCapabilities.None"/> when it provides
        /// all of them.
        /// </summary>
        public static TestCapabilities Missing(TestCapabilities required)
        {
            return required & ~Available;
        }
    }
}
