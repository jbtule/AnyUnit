namespace MsTestTests
{
    // Passed as a row argument purely so it shows up in the generated test
    // name (AnyUnit.Run.Test's constructor appends each argument's
    // ToString()), which is what ConventionTestProcessor's ResultMatchesName
    // actually reads. Same trick every other style's self-tests use.
    public enum Expected
    {
        _Error,
        _Success,
        _Ignore,
        _Fail,
        _NoError
    }
}
