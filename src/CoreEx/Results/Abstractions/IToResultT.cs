namespace CoreEx.Results.Abstractions;

/// <summary>
/// Enables the creation of a <see cref="Result{T}"/> representing the current state.
/// </summary>
public interface IToResult<T> : IToResult
{
    /// <inheritdoc/>
    Result IToResult.ToResult() => ToResult().AsResult();

    /// <summary>
    /// Creates a <see cref="Result{T}"/> representing the current state.
    /// </summary>
    new Result<T> ToResult();
}