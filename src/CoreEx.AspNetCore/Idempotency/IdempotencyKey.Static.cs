namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides <see cref="IIdempotencyProvider"/> utility.
/// </summary>
public partial class IdempotencyKey
{
    private const string _errorType = "idempotency-key";

    /// <summary>
    /// Checks whether a legacy bare idempotency key is considered valid.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to validate.</param>
    /// <param name="exception">The resulting validation exception where the idempotency key is invalid.</param>
    /// <returns><see langword="true"/> indicates that the idempotency key is valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This legacy compatibility form must be between 8 and 128 characters in length and consist of only letters, numbers, hyphens and underscores. It is not the standard Structured Field representation.</remarks>
    public static bool IsIdempotencyKeyValid([NotNullWhen(true)] string? idempotencyKey, [NotNullWhen(false)] out ValidationException? exception)
    {
        if (string.IsNullOrEmpty(idempotencyKey) || !_legacyIdempotencyKeyRegex().IsMatch(idempotencyKey))
        {
            exception = CreateInvalidHeaderException();
            return false;
        }

        exception = null;
        return true;
    }

    /// <summary>
    /// Parses the standard quoted Structured Field string form or, when permitted, the legacy bare-key form.
    /// </summary>
    /// <param name="header">The raw header value.</param>
    /// <param name="strict">Whether to reject legacy bare keys.</param>
    /// <param name="key">The decoded logical key.</param>
    /// <returns><see langword="true"/> when the value contains one valid idempotency key.</returns>
    public static bool TryParseHeader(string? header, bool strict, [NotNullWhen(true)] out string? key)
    {
        key = null;
        if (string.IsNullOrEmpty(header))
            return false;

        var value = header.Trim(' ', '\t');
        if (value.Length == 0)
            return false;

        if (value[0] != '"')
        {
            if (strict || !IsIdempotencyKeyValid(value, out _))
                return false;

            key = value;
            return true;
        }

        var match = _quotedIdempotencyKeyRegex().Match(value);
        if (!match.Success)
            return false;

        var decoded = _quotedIdempotencyKeyEscapeRegex().Replace(match.Groups["key"].Value, "$1");
        if (!IsDecodedKeyValid(decoded))
            return false;

        key = decoded;
        return true;
    }

    /// <summary>
    /// Creates a <see cref="ValidationException"/> to indicate that the idempotency key is required.
    /// </summary>
    /// <returns>The resulting validation exception.</returns>
    public static ValidationException CreateIdempotencyKeyRequiredException()
        => new ValidationException($"The '{HttpNames.IdempotencyKeyHeaderName}' header is required.")
        .WithErrorType(_errorType)
        .WithErrorCode("header-is-required")
        .WithDetail("Provide one idempotency key as a quoted Structured Field string. For compatibility, the server may also accept the legacy bare-key format.");

    /// <summary>
    /// Creates a <see cref="ValidationException"/> for a malformed or disallowed idempotency header.
    /// </summary>
    /// <returns>The resulting validation exception.</returns>
    public static ValidationException CreateInvalidHeaderException()
        => new ValidationException($"The '{HttpNames.IdempotencyKeyHeaderName}' header is invalid.")
        .WithErrorType(_errorType)
        .WithErrorCode("header-is-invalid")
        .WithDetail("Provide one valid quoted Structured Field string key; relaxed compatibility mode also accepts one 8-128 character legacy bare key using letters, numbers, hyphens or underscores.");

    /// <summary>
    /// Creates a <see cref="ConflictException"/> to indicate that the idempotent operation is in progress.
    /// </summary>
    /// <param name="retryAfter">The optional retry-after interval; defaults to <see cref="TransientException.DefaultRetryAfter"/>.</param>
    /// <returns>The resulting conflict exception.</returns>
    public static ConflictException CreateInProgressException(TimeSpan? retryAfter = null)
        => new ConflictException($"An operation with the specified '{HttpNames.IdempotencyKeyHeaderName}' header is already in progress.")
        .WithErrorType(_errorType)
        .WithErrorCode("is-in-progress")
        .WithDetail("An operation with the specified idempotency key is already in progress; please wait for its completion before retrying.")
        .AsTransient(retryAfter);

    /// <summary>
    /// Creates a <see cref="ConflictException"/> for an operation whose outcome cannot be established.
    /// </summary>
    /// <returns>The resulting conflict exception.</returns>
    public static ConflictException CreateIndeterminateException()
        => new ConflictException($"The outcome of the operation associated with '{HttpNames.IdempotencyKeyHeaderName}' cannot be established.")
        .WithErrorType(_errorType)
        .WithErrorCode("outcome-indeterminate")
        .WithDetail("The original operation may have completed. Automatic retry is blocked until its outcome is reconciled; waiting or choosing another key does not make re-execution safe.");

    /// <summary>
    /// Creates a <see cref="ConflictException"/> to indicate that the idempotency key has already been used for a different request.
    /// </summary>
    /// <returns>The resulting conflict exception.</returns>
    public static ConflictException CreateUsedForDifferentRequestException()
        => new ConflictException($"The '{HttpNames.IdempotencyKeyHeaderName}' header has already been used for a different request.")
        .WithStatusCode(HttpStatusCode.UnprocessableContent)
        .WithErrorType(_errorType)
        .WithErrorCode("different-request")
        .WithDetail("The specified idempotency key has already been used for a different request; reuse of idempotency keys is not allowed.");

    /// <summary>
    /// Creates a <see cref="ConflictException"/> to indicate that the response associated with the idempotency key was too large can not be replayed.
    /// </summary>
    /// <returns>The resulting conflict exception.</returns>
    public static ConflictException CreateResponseTooLargeException()
        => new ConflictException($"The response associated with the specified '{HttpNames.IdempotencyKeyHeaderName}' is no longer available.")
        .WithErrorType(_errorType)
        .WithErrorCode("response-unavailable")
        .WithDetail("The operation completed, but its response exceeded the replay limit and is not available. The operation will not be executed again.");

    /// <summary>
    /// Creates a service-unavailable failure while retaining the backend exception for diagnostics.
    /// </summary>
    /// <param name="innerException">The backend failure.</param>
    /// <returns>The resulting transient exception.</returns>
    public static TransientException CreateStoreUnavailableException(Exception innerException)
        => new TransientException(null, innerException)
            .WithErrorType(_errorType)
            .WithErrorCode("store-unavailable");

    /// <summary>
    /// Checks the CoreEx decoded-key length.
    /// </summary>
    private static bool IsDecodedKeyValid(string value)
    {
        return value.Length is >= 8 and <= 128;
    }

    [GeneratedRegex("\\A[A-Za-z0-9_-]{8,128}\\z", RegexOptions.Compiled)]
    private static partial Regex _legacyIdempotencyKeyRegex();

    [GeneratedRegex(@"\A""(?<key>(?:[\x20-\x21\x23-\x5B\x5D-\x7E]|\\[""\\]){8,256})""\z", RegexOptions.Compiled)]
    private static partial Regex _quotedIdempotencyKeyRegex();

    [GeneratedRegex(@"\\([""\\])", RegexOptions.Compiled)]
    private static partial Regex _quotedIdempotencyKeyEscapeRegex();
}
