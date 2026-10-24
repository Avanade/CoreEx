namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Preserves an adapted property's null state independently of its typed value.
/// </summary>
internal interface IPropertyContextNullState
{
    /// <summary>
    /// Gets the optional function that reads the originating property's current null state.
    /// </summary>
    Func<bool>? GetIsValueNull { get; }
}
