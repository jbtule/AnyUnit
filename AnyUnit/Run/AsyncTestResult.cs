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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyUnit.Util;

namespace AnyUnit.Run
{
    /// <summary>
    /// Awaits whatever a test method handed back, so an asynchronous test
    /// body is actually finished - and its failure actually observed -
    /// before the engine decides the test's result.
    /// </summary>
    /// <remarks>
    /// Before this existed, <see cref="TestInvoker"/>'s returned object was
    /// only ever inspected for <c>bool</c> and <see cref="IReturnedResult"/>.
    /// An <c>async Task</c> test method therefore had its Task dropped on the
    /// floor unawaited: the body might not have run to completion at all, and
    /// - worse - an async method NEVER throws out of the call itself. Every
    /// exception it raises, including an AssertionException from a failed
    /// assert, is captured into the returned Task instead. So the invoke
    /// returned normally, nothing was in the exception list, and every async
    /// test passed no matter what it asserted. Silently.
    ///
    /// Deliberately kept synchronous. Making this path async would mean
    /// plumbing async up through Test.Run, Fixture, and Runner.RunAll, and
    /// from there into all five hosts (AnyUnit.TestingPlatform, the net10 /
    /// net48 / browser-wasm runners, and Runner/Bootstrap) - an enormous
    /// ripple for a core that is otherwise deliberately plain reflection.
    /// Blocking here is safe because Test.Run already runs this whole call
    /// on a dedicated ThreadPool thread and does its [Timeout] WaitHandle
    /// wait OUTSIDE it (see Test.Run) - blocking the test's own thread is
    /// exactly what that structure is for, and a hung await is timed out by
    /// the same machinery that already times out a hung busy-loop.
    ///
    /// The one runtime where that is not true is single-threaded
    /// browser-wasm; see <see cref="Block"/>.
    /// </remarks>
    internal static class AsyncTestResult
    {
        /// <summary>
        /// If <paramref name="result"/> is awaitable, wait for it and return
        /// the value it produced (so <c>async Task&lt;bool&gt;</c> still gets
        /// the engine's bool handling, and an awaited IReturnedResult still
        /// gets its own). Anything else is passed straight back untouched.
        /// Any exception the test body raised is rethrown from here, so the
        /// caller's existing catch classifies it exactly as it would have
        /// classified a synchronous throw.
        /// </summary>
        public static object Unwrap(object result)
        {
            var task = ToTask(result);
            if (task == null)
            {
                return result;
            }

            Block(task);
            return TaskValue(task);
        }

        // ValueTask/ValueTask<T>.AsTask() are looked up by name below (see
        // the comment there for why not a direct reference). Under Native
        // AOT that lookup only succeeds if the method survived trimming;
        // these keep it, resolved by name in the target app since this
        // assembly cannot name the type.
        [DynamicDependency("AsTask", "System.Threading.Tasks.ValueTask", "System.Runtime")]
        [DynamicDependency("AsTask", "System.Threading.Tasks.ValueTask`1", "System.Runtime")]
        private static Task ToTask(object result)
        {
            if (result == null)
            {
                return null;
            }

            var task = result as Task;
            if (task != null)
            {
                return task;
            }

            var type = result.GetType();
            var name = type.FullName ?? String.Empty;

            // ValueTask / ValueTask<T> are matched by name and adapted via
            // AsTask() rather than referenced directly, because they simply
            // are not part of netstandard2.0 - they live in the separate
            // System.Threading.Tasks.Extensions package. Taking that
            // PackageReference on the core would push an extra assembly out
            // to every consumer (and onto the net48 runner's own
            // embedded-dependency loading path) purely to name a type the
            // core never constructs. Duck typing costs one string compare
            // per test and no dependency at all.
            if (name.StartsWith("System.Threading.Tasks.ValueTask", StringComparison.Ordinal))
            {
                var asTask = type.Method("AsTask", new Type[0]);
                if (asTask != null)
                {
                    return asTask.Invoke(result, null) as Task;
                }
            }

            // F# Async<'T>. Same no-dependency reasoning as ValueTask above -
            // the core must not take an FSharp.Core reference - but with an
            // extra wrinkle: WHICH start function is used matters.
            // StartImmediateAsTask runs the workflow on the CURRENT thread
            // until its first genuine suspension point, so a workflow that
            // never really suspends (by far the common case for a test body)
            // comes back as an already-completed Task. Async.StartAsTask
            // would instead queue the whole thing to the ThreadPool, which
            // on single-threaded browser-wasm means it would never run at
            // all - see Block below.
            if (name.StartsWith("Microsoft.FSharp.Control.FSharpAsync`1", StringComparison.Ordinal))
            {
                return FSharpAsyncToTask(result, type);
            }

            return null;
        }

        // Reflection over FSharp.Core, which this assembly cannot reference
        // (see ToTask). What the analyzer flags here is real - the type is
        // found by name and the method instantiated at run time - and is
        // answered on the build side rather than here: AnyUnit.TestingPlatform.
        // targets hands ILC an rd.xml asking for StartImmediateAsTask<unit>
        // and <bool> whenever FSharp.Core is referenced, so under Native AOT
        // those instantiations exist and MakeGenericMethod finds them. Any
        // other 'T, or a trimmed FSharp.Core, is a loud Error below naming
        // that file - never a silently un-awaited test.
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = Trimming.FSharpAsync)]
        [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = Trimming.FSharpAsync)]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = Trimming.FSharpAsync)]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = Trimming.FSharpAsync)]
        private static Task FSharpAsyncToTask(object result, Type type)
        {
            // Every failure past the name check throws rather than returning
            // null. A null here means "not something to await", and the
            // caller then treats the Async<'T> object as the test's plain
            // return value: the body never runs to completion, whatever it
            // would have asserted is never observed, and the test passes.
            // That is exactly the bug awaiting was added to fix, and under
            // Native AOT with FSharp.Core trimmed it came back as four
            // silently-green F# tests (confirmed with FsUnitTests.Mtp).
            var asyncModule = type.GetTypeInfo().Assembly.GetType("Microsoft.FSharp.Control.FSharpAsync");
            var start = asyncModule == null
                ? null
                : asyncModule
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(it => it.Name == "StartImmediateAsTask"
                                          && it.IsGenericMethodDefinition
                                          && it.GetParameters().Length == 2);
            if (start == null)
            {
                throw new NotSupportedException(
                    "This test returned an F# " + type.Name + " but FSharp.Core's Async.StartImmediateAsTask could not be found "
                    + "(FSharp.Core " + type.GetTypeInfo().Assembly.GetName().Version + "). "
                    + FSharpAsyncHint);
            }

            var resultType = type.GenericArgs()[0];
            try
            {
                // Second argument is FSharpOption<CancellationToken>; null is
                // None, i.e. "no cancellation token", the same as omitting the
                // optional parameter from F#.
                return start.MakeGenericMethod(resultType)
                            .Invoke(null, new object[] { result, null }) as Task;
            }
            catch (NotSupportedException ex)
            {
                // Native AOT: no compiled instantiation for this 'T.
                throw new NotSupportedException(
                    "This test returned an F# Async<" + resultType.Name + ">, and Async.StartImmediateAsTask<" + resultType.Name
                    + "> has no native code in this Native AOT build. " + FSharpAsyncHint, ex);
            }
        }

        private const string FSharpAsyncHint =
            "Under Native AOT, AnyUnit.TestingPlatform supplies the Async<unit> and Async<bool> instantiations "
            + "(build/FSharpAsync.rd.xml); for another result type add an RdXmlFile item naming it, "
            + "or return a Task (async { ... } |> Async.StartImmediateAsTask) instead.";

        private static void Block(Task task)
        {
            // Single-threaded browser-wasm (Utility.IsSingleThreadedRuntime)
            // is the one place blocking is not an option. There is no second
            // thread, and a Task's continuation only ever runs once this
            // thread yields back to the browser's event loop - which a
            // synchronous block, by definition, never does. See
            // Utility.IsSingleThreadedRuntime's own comment, and Test.Run,
            // which already runs the test body inline on that runtime for
            // exactly this reason.
            //
            // Settled by a real headless-browser run, not by reasoning about
            // it (Runner/Platforms/browser-wasm-runner over BasicTests, the
            // same thing build.yml's test-browser-wasm job does), with
            // instrumentation temporarily added right here:
            //
            //   * Every async test whose awaits all complete synchronously -
            //     `await Task.FromResult(0)`, `await Task.CompletedTask`,
            //     an already-finished Task - arrives here with
            //     IsCompleted=True. Confirmed for all seven such tests in
            //     BasicTests. Those need no waiting at all, and their
            //     Fail/Error/Ignore/NoError classification comes out
            //     identical to the desktop legs.
            //   * A test that genuinely suspends (`await Task.Delay(1)`)
            //     arrives with IsCompleted=False and STAYS that way: a
            //     deliberate 2000ms busy-spin on `!task.IsCompleted` right
            //     here ended with IsCompleted=False. Spinning cannot pump
            //     the browser's event loop, so there is no "pump until
            //     completion" option to take - the continuation is queued
            //     behind a yield this thread will never perform.
            //
            // Hence: read an already-completed task (no blocking involved),
            // and for anything else fail fast with a clear Error. A hang
            // here is far worse than an Error - it wedges the browser page
            // and takes the entire run with it, with nothing reported at
            // all, which is precisely the failure mode Test.Run's inline
            // branch already exists to avoid.
            //
            // Tests that really do need to suspend can still be written -
            // they just have to declare it, with
            // [RequiresCapability(TestCapabilities.AsyncYield)], and the
            // engine reports them Ignored here instead of reaching this at
            // all. Landing in this branch therefore means the test did NOT
            // declare the requirement, which is exactly when a clear error
            // beats a silent skip.
            if (Utility.IsSingleThreadedRuntime && !task.IsCompleted)
            {
                throw new NotSupportedException(
                    "This test returned an asynchronous operation that had not finished, and this "
                    + "runtime is single-threaded (browser WebAssembly without threads), so it can "
                    + "never finish: its continuation needs this thread to yield back to the "
                    + "browser's event loop, which a test run cannot do. Either make the test's "
                    + "awaits complete synchronously, or declare the requirement with "
                    + "[RequiresCapability(TestCapabilities.AsyncYield)], which reports the "
                    + "test as Ignored on platforms that cannot provide it.");
            }

            // GetAwaiter().GetResult() rather than Wait(): Wait() wraps
            // whatever the body threw in an AggregateException, which would
            // then reach TestCycleExceptions.GetResult as a plain unknown
            // exception type - turning every failed async assertion into an
            // Error instead of a Fail, and every async Assert.Ignore into an
            // Error instead of an Ignore. GetResult rethrows the original
            // exception, unwrapped and with its stack trace preserved, which
            // is precisely what the classification in
            // TestCycleExceptions.GetResult needs to see.
            task.GetAwaiter().GetResult();
        }

        // Under Native AOT nothing in the image calls Task<T>.Result
        // statically (this is the only reader, and it goes through
        // reflection), so without this the getter is trimmed away and
        // InstanceProperty("Result") comes back null: confirmed for real
        // with BasicTests.Mtp published PublishAot=true - every async test
        // errored with a NullReferenceException here, and with just a null
        // guard the Task<bool> ones silently lost their return value.
        // DynamicDependency on the open generic keeps get_Result on every
        // instantiation. The attribute itself is the internal copy in
        // Util/DynamicDependencyAttribute.cs (netstandard2.0 has none).
        [DynamicDependency("get_Result", typeof(Task<>))]
        private static object TaskValue(Task task)
        {
            // Task<T>.Result, read only after the task completed successfully
            // (Block above has already rethrown otherwise). Reflection rather
            // than a generic cast because T isn't known here - the test method
            // could return Task<bool>, Task<SomeIReturnedResult>, or anything
            // else at all.
            var type = task.GetType();
            while (type != null)
            {
                if (type.MatchesGenericDef(typeof(Task<>)))
                {
                    return type.InstanceProperty("Result").GetValue(task, null);
                }
                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }
    }
}
