namespace PAnyUnitStyle.FsUnit


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

    let ofExactType<'a> = ExactTypeConstraint(typeof<'a>)

    let instanceOfType<'a> = InstanceOfTypeConstraint(typeof<'a>)

    let not' x = NotConstraint(x)
