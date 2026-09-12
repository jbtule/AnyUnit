namespace ComboTests
{
    /// <summary>
    /// Embedded as a trailing parameter on rows below so its value lands
    /// in AnyUnit.Run.Test's own args-based name suffix - same naming
    /// trick XunitTests/NunitTests already use, letting
    /// WhoTestsTheTesters/ConventionTestProcessor's plain substring check
    /// (_Success/_Fail/etc. in Result.Test.Name) keep working per-row.
    /// </summary>
    public enum Expected
    {
        _Error,
        _Success,
        _Ignore,
        _Fail,
        _NoError
    }
}
