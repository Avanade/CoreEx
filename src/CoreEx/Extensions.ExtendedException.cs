namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Gets the extension name for the <see cref="ExtendedException"/> <see cref="ExtendedException.Extensions"/> dictionary to use for the <see cref="WithKey{TException, TKey}(TException, TKey)"/> value.
    /// </summary>
    public const string KeyExtensionName = "key";

    /// <summary>
    /// Sets (overrides) the <paramref name="exception"/> <see cref="ExtendedException.ErrorCode"/> to the specified <paramref name="errorCode"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="errorCode">The error code.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    public static TException WithErrorCode<TException>(this TException exception, string errorCode) where TException : ExtendedException
        => exception.ThrowIfNull().Adjust(ex => ex.ErrorCode = errorCode.ThrowIfNullOrEmpty());

    /// <summary>
    /// Sets (overrides) the <paramref name="exception"/> <see cref="ExtendedException.ErrorType"/> to the specified <paramref name="errorType"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="errorType">The error type.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    public static TException WithErrorType<TException>(this TException exception, string errorType) where TException : ExtendedException
        => exception.ThrowIfNull().Adjust(ex => ex.ErrorType = errorType.ThrowIfNullOrEmpty());

    /// <summary>
    /// Sets (overrides) the <paramref name="exception"/> <see cref="ExtendedException.StatusCode"/> to the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    public static TException WithStatusCode<TException>(this TException exception, HttpStatusCode statusCode) where TException : ExtendedException
        => exception.ThrowIfNull().Adjust(ex => ex.StatusCode = statusCode);

    /// <summary>
    /// Sets (overrides) the <paramref name="exception"/> <see cref="ExtendedException.Detail"/> to the specified <paramref name="detail"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="detail">The error detail.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    public static TException WithDetail<TException>(this TException exception, string detail) where TException : ExtendedException
        => exception.ThrowIfNull().Adjust(ex => ex.Detail = detail);

    /// <summary>
    /// Adds (overrides) the specified <paramref name="key"/> (using <see cref="KeyExtensionName"/> within the <see cref="ExtendedException.Extensions"/> dictionary).
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The <paramref name="key"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="key">The key value.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is a convenience method for adding a key using the <see cref="KeyExtensionName"/> (see <see cref="WithExtension{TException, T}(TException, string, T)"/>).</remarks>
    public static TException WithKey<TException, TKey>(this TException exception, TKey key) where TException : ExtendedException
        => exception.ThrowIfNull().WithExtension(KeyExtensionName, key);

    /// <summary>
    /// Adds (overrides) the specified <paramref name="name"/> and <paramref name="value"/> pair within the <see cref="ExtendedException.Extensions"/> dictionary.
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="T">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="name">The extension name.</param>
    /// <param name="value">The extension value.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    public static TException WithExtension<TException, T>(this TException exception, string name, T value) where TException : ExtendedException
        => exception.ThrowIfNull().Adjust(ex => ex.Extensions[name.ThrowIfNullOrEmpty()] = value.ThrowIfNull());

    /// <summary>
    /// Sets (overrides) the <paramref name="exception"/> <see cref="ExtendedException.IsTransient"/> to <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="ExtendedException{TSelf}"/> <see cref="Type"/>.</typeparam>
    /// <param name="exception">The <see cref="ExtendedException{TSelf}"/>.</param>
    /// <param name="retryAfter">The optional retry-after interval; defaults to <see cref="TransientException.DefaultRetryAfter"/>.</param>
    /// <returns>The <paramref name="exception"/> to support fluent-style method-chaining.</returns>
    public static TException AsTransient<TException>(this TException exception, TimeSpan? retryAfter = null) where TException : ExtendedException
        => exception.ThrowIfNull().Adjust(ex => ex.IsTransient = true).Adjust(ex => ex.RetryAfter = retryAfter ?? TransientException.DefaultRetryAfter);
}