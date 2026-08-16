namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Enables the <see cref="WebApi{TResult}"/> request options.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface IWebApiRequestOptions<TRequest>
{
    /// <summary>
    /// Indicates whether to automatically <see cref="Cleaner.Clean{T}(T, CleanArgs)"/> the request body value on first access.
    /// </summary>
    /// <remarks><see cref="CleanArgs.Default"/> will be used to ensure the root and nested values are not defaulted (overridden) changing the request data shape.</remarks>
    bool AutoCleanValue { get; set; }

    /// <summary>
    /// Gets the request value (from body) or <see langword="default"/>.
    /// </summary>
    TRequest? ValueOrDefault { get; }

    /// <summary>
    /// Gets the request value (from body) where not <see langword="default"/>; otherwise, results in a corresponding <see cref="ValidationException"/>.
    /// </summary>
    [NotNull]
    TRequest Value { get; }
}
