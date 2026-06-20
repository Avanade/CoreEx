namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Enables the <see cref="WebApi{TResult}"/> request options.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IWebApiRequestOptions<TRequest>
{
    /// <summary>
    /// Gets the request value or <see langword="default"/>.
    /// </summary>
    TRequest? ValueOrDefault { get; }

    /// <summary>
    /// Gets the request value where not <see langword="default"/>; otherwise, results in a corresponding <see cref="ValidationException"/> (see <see cref="Validation.ValidatorExtensions.Required{T}(T, string?, LText?)"/>).
    /// </summary>
    [NotNull]
    TRequest Value { get; }
}