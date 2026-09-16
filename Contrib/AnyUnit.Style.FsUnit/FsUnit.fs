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

// `should`/`shouldFail` reach the running test through AnyUnit.Run.
// AmbientTest (set by the engine around every test body), so a bare
// `x |> should equal y` in a module-level test asserts against that
// test's own helper - counted, so a test with no assertion still reports
// NoError. Only outside a running test do they fall back to the obsolete
// Assert.GlobalStyle, which C# would auto-suppress the warning for in an
// obsolete caller but F# does not; hence the nowarn.
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

    /// The running test's IAssert, or - only outside a test body - the
    /// global fallback.
    let internal currentAssert () : AnyUnit.IAssert =
        match AnyUnit.Run.AmbientTest.Current with
        | null -> AnyUnit.Run.Assert.GlobalStyle
        | helper -> helper.Assert

    /// `actual |> should equal expected` - asserts through the running
    /// test (see the top of this file). `this.should` is the same thing
    /// spelled explicitly, for a test that has a `this`.
    let should (f : 'a -> #Constraint) x (y : obj) =
        shouldHelper (currentAssert ()) f x y
    
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
          
    let shouldFail (f : unit -> unit) =
        shouldFailHelper (currentAssert ()) f

    let endWith (s:string) = EndsWithConstraint s

    let startWith (s:string) = StartsWithConstraint s

    // FsUnit's own name for "string contains" - `contain` already reaches
    // SubstringConstraint for a string actual, but a ported suite says
    // haveSubstring, and shouldn't have to be edited to say otherwise.
    let haveSubstring (s:string) = SubstringConstraint s

    let ofExactType<'a> = ExactTypeConstraint(typeof<'a>)

    let instanceOfType<'a> = InstanceOfTypeConstraint(typeof<'a>)

    let not' x = NotConstraint(x)
