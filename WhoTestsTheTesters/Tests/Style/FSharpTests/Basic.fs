/// Same coverage as WhoTestsTheTesters/Tests/BasicTests/Basic.cs and
/// .../Style/FsUnitTests/BasicTests.fs - one test per ResultKind, named
/// per WhoTestsTheTesters/ConventionTestProcessor's own convention
/// (_Success/_Fail/_Error/_Ignore/_NoError substrings, matched against
/// Result.Test.Name - a Contains check, so it still works fine even
/// with the current "get_xxx" property-getter name Discovery.fs reports
/// tests under). Only AnyUnit's own IAssert primitives (True/False/
/// Fail/Ignore) are used - no style package's extension-method
/// assertion vocabulary (Equal/Contains/etc.) needed for these.
module FSharpTests.Basic

open AnyUnit.Style.FSharp.Test

let testTrue_Success = test {
    let! Assert = assertion
    let! Log = log
    Log.Write("This is just a hardcoded true")
    Assert.True(true)
}

let testFalse_Success = test {
    let! Assert = assertion
    Assert.False(false)
}

let testFalse_Fail = test {
    let! Assert = assertion
    Assert.False(true, "Expected False")
}

let testTrue_Fail = test {
    let! Assert = assertion
    Assert.True(false, "Expected True")
}

let testFail_Fail = test {
    let! Assert = assertion
    Assert.Fail("Just Fail")
}

let testNothing_NoError = test { () }

let test_Error = test { failwith "This should be an error." }

let test_Ignore = test {
    let! Assert = assertion
    Assert.Ignore("Ignoring...")
}
