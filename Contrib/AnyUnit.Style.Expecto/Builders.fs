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

open System.Threading.Tasks

// See Types.fs: AnyUnit.Run stays unopened so its own `Test` cannot
// shadow this style's.

/// Expecto's own constructors, same names and same shapes, so
/// `testCase "x" <| fun () -> ...` carries over unchanged.
///
/// AutoOpen so a ported file needs one `open AnyUnit.Style.Expecto` where
/// it had `open Expecto`.
///
/// No ftestCase/ftestList/FTests: see Test's own comment - focusing is a
/// debugging aid that is a bug once committed, and leaving it out makes a
/// stray one a compile error rather than a silent change in what runs.
[<AutoOpen>]
module Builders =

    let testCase (name: string) (code: unit -> unit) = TestCase(name, Sync code, false)

    /// Pending: compiled and reported, but not run - AnyUnit reports it Ignored.
    let ptestCase (name: string) (code: unit -> unit) = TestCase(name, Sync code, true)

    let testCaseAsync (name: string) (code: Async<unit>) = TestCase(name, AsyncCode code, false)

    let ptestCaseAsync (name: string) (code: Async<unit>) = TestCase(name, AsyncCode code, true)

    let testCaseTask (name: string) (code: unit -> Task) = TestCase(name, TaskCode code, false)

    let ptestCaseTask (name: string) (code: unit -> Task) = TestCase(name, TaskCode code, true)

    let testList (name: string) (tests: Test list) = TestList(name, tests, false)

    /// Pending for every test beneath it, however deeply nested.
    let ptestList (name: string) (tests: Test list) = TestList(name, tests, true)

    /// AnyUnit extension, no Expecto equivalent: declare that a test - or
    /// a whole list - needs a runtime facility not every platform has.
    ///
    ///     requires TestCapabilities.AsyncYield (
    ///         testCaseAsync "genuinely suspends" <| async { ... })
    ///
    /// Attribute-based styles write [RequiresCapability(...)] instead;
    /// this style has no method or class to attach one to, so the
    /// requirement composes into the tree like everything else here.
    let requires (capability: AnyUnit.TestCapabilities) (test: Test) =
        TestRequires(capability, test)

    /// Unconditional failure, as Expecto's own top-level `failtest`/`failtestf`.
    let failtest (message: string) : 'a = Expect.failtest message
    let failtestf (format: Printf.StringFormat<'T, 'a>) : 'T = Tests.failtestf format
