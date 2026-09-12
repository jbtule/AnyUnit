namespace AnyUnit.Run.Attributes
{
    /// <summary>
    /// Implemented by a method-level, one-row-per-instance data attribute
    /// (xUnit's InlineDataAttribute, NUnit's TestCaseAttribute) so any
    /// style's primary TestAttributeBase can recognize a DIFFERENT
    /// style's row attribute on the same method, without a project/
    /// assembly reference to that style - only this core interface,
    /// which every style already references.
    /// </summary>
    public interface IRowInlineParameter
    {
        object[] Arguments { get; }
    }
}
