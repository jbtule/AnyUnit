//
//  This is an altered version of FsUnit's own `should` operator
//  vocabulary, adapted to assert through AnyUnit's IAssert rather than
//  NUnit's static Assert.
//
//  Copyright (c) 2010 Raymond Vernagus
//  Licensed under the MIT license - see License.txt in this directory
//  for the full text and notice.
//
//  Alterations Copyright 2017-2026 AnyUnit Contributors, Apache-2.0.
//

namespace AnyUnit.Style.FsUnit

// `should`/`shouldFail` below are deliberately marked [<Obsolete>] themselves
// (steering callers toward the newer `this.should`/`this.shouldFail`
// instance-style API), but still need to reference the also-Obsolete
// AnyUnit.Run.Assert.GlobalStyle internally to keep working during the
// deprecation period. C# auto-suppresses an Obsolete-member warning when
// the calling member is itself Obsolete; F# doesn't have that suppression,
// so FS0044 still fires here even though the usage is intentional and
// already flagged at the call site by the containing function's own
// attribute.
#nowarn "44"

open AnyUnit
open AnyUnit.Constraints
open AnyUnit.Constraints.Pieces

[<AutoOpen>]
module TopLevelOperators =
    let Null = NullConstraint()

    let Empty = EmptyConstraint()

    let EmptyString = EmptyStringConstraint()

    let NullOrEmptyString = NullOrEmptyStringConstraint()

    let True = TrueConstraint()

    let False = FalseConstraint()

    let NaN = NaNConstraint()

    let unique = UniqueItemsConstraint()

    let internal shouldHelper (asserter:AnyUnit.IAssert) (f : 'a -> #Constraint) x (y : obj) =
        let c = f x
        let y =
            match y with
            | :? (unit -> unit) -> box (TestDelegate(y :?> unit -> unit))
            | _ -> y
        asserter.That(y, c)

    [<System.Obsolete("Uses Global Assertion, so anyunit goes into global assertion mode,  use `this.should` instead")>]
    let should (f : 'a -> #Constraint) x (y : obj) =
        shouldHelper AnyUnit.Run.Assert.GlobalStyle f x y
    
    let equal x = EqualConstraint(x)

    let equalWithin tolerance x = equal(x).Within tolerance

    let contain x = ContainsConstraint(x)

    let haveLength n = Has.Length.EqualTo(n)

    let haveCount n = Has.Count.EqualTo(n)

    let be = id

    let sameAs x = SameAsConstraint(x)

    let throw = Throws.TypeOf

    let greaterThan x = GreaterThanConstraint(x)

    let greaterThanOrEqualTo x = GreaterThanOrEqualConstraint(x)

    let lessThan x = LessThanConstraint(x)

    let lessThanOrEqualTo x = LessThanOrEqualConstraint(x)

    let internal shouldFailHelper (asserter:AnyUnit.IAssert) (f : unit -> unit) =
        TestDelegate(f) |> shouldHelper asserter throw typeof<AssertionException>
          
    [<System.Obsolete("Uses Global Assertion, so anyunit goes into global assertion mode, use `this.shouldFail` instead")>]
    let shouldFail (f : unit -> unit) =
        shouldFailHelper AnyUnit.Run.Assert.GlobalStyle f

    let endWith (s:string) = EndsWithConstraint s

    let startWith (s:string) = StartsWithConstraint s

    // FsUnit's own name for "string contains" - `contain` already reaches
    // SubstringConstraint for a string actual, but a ported suite says
    // haveSubstring, and shouldn't have to be edited to say otherwise.
    let haveSubstring (s:string) = SubstringConstraint s

    let ofExactType<'a> = ExactTypeConstraint(typeof<'a>)

    let instanceOfType<'a> = InstanceOfTypeConstraint(typeof<'a>)

    let not' x = NotConstraint(x)
