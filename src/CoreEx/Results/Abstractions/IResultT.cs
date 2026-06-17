namespace CoreEx.Results.Abstractions;

/// <summary>
/// Enables the use of a <c>Result</c> type with a <see cref="Value"/> to represent the success outcome of an operation.
/// </summary>
/// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
public interface IResult<T> : IResult
{
    /// <summary>
    /// Gets the underlying value where <see cref="IResult.IsSuccess"/>.
    /// </summary>
    /// <remarks>Where <see cref="IResult.IsFailure"/> then the corresponding <see cref="IResult.Error"/> will be thrown.</remarks>
    new T Value { get; }
}