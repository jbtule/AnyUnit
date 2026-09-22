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

namespace AnyUnit.Style.Expecto

open System
open System.Threading.Tasks

// AnyUnit.Run is NOT opened here: it contains its own `Test` - the
// engine's runnable test - which would shadow the union defined below.
// The one type needed from it is written out in full instead.

/// The body of a single test case. Three shapes rather than one, because
/// Expecto's own `testCase`/`testCaseAsync`/`testCaseTask` are genuinely
/// three different things and flattening them to `unit -> unit` would mean
/// either blocking here (wrong - the engine owns that decision, see
/// AnyUnit.Run.AsyncTestResult) or silently dropping the asynchronous half.
type TestCode =
    | Sync of (unit -> unit)
    | AsyncCode of Async<unit>
    | TaskCode of (unit -> Task)

/// A test, or a tree of them. Values, not attributed methods - the whole
/// point of the style.
///
/// `pending` rides on both cases because Expecto's `ptestCase`/`ptestList`
/// mark a test (or a whole list) as not-to-be-run while keeping it
/// compiled and visible, which maps exactly onto AnyUnit's Ignore. There
/// is deliberately no focused (`ftest*`) counterpart: focusing is a
/// hand-editing debugging aid that is a bug once committed - Expecto
/// itself ships `--fail-on-focused-tests` to catch exactly that - so
/// omitting it means a stray `ftestCase` fails to COMPILE here rather than
/// quietly changing which tests run.
type Test =
    | TestCase of name: string * code: TestCode * pending: bool
    | TestList of name: string * tests: Test list * pending: bool
    /// AnyUnit's own addition, with no Expecto counterpart: everything
    /// beneath this needs a runtime facility the platform may not have
    /// (see AnyUnit.TestCapabilities), and is reported Ignored where
    /// it is missing rather than failing for an unrelated reason.
    ///
    /// It exists as a TREE NODE because this style has nowhere to put
    /// [RequiresCapability]: an attribute needs a method or a class to
    /// sit on, and a test here is a value in a list. The combinator is
    /// the value-based equivalent - see Builders.requires.
    | TestRequires of capability: AnyUnit.TestCapabilities * test: Test

/// Marks a `let` binding as a suite root, the same as Expecto's own
/// `[<Tests>]`.
///
/// Applied to a VALUE binding the attribute lands on the compiler-
/// generated property rather than on any reflectable method - confirmed
/// directly, and the reason AnyUnit.Style.FSharp's Discovery.fs reads
/// PropertyInfo too. Discovery here does the same.
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Method, AllowMultiple = false)>]
type TestsAttribute() =
    inherit Attribute()

/// Per-binding escape hatch: every leaf under this `[<Tests>]` binding
/// that completes without throwing reports Success even if it made no
/// Expect call. See ExpectoStyleAttribute.CompletionIsPass for the
/// whole-assembly form and for what is given up.
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Method, AllowMultiple = false)>]
type CompletionIsPassAttribute() =
    inherit Attribute()
