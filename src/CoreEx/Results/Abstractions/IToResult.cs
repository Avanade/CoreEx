namespace CoreEx.Results.Abstractions;

/// <summary>
/// Enables the creation of a <see cref="Result"/> representing the current state.
/// </summary>
public interface IToResult
{
    /// <summary>
    /// Creates a <see cref="Result"/> representing the current state.
    /// </summary>
    Result ToResult();
}