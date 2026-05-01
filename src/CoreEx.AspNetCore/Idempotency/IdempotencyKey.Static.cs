namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides <see cref="IIdempotencyProvider"/> utility.
/// </summary>
public partial class IdempotencyKey
{
    private const string _errorType = "idempotency-key";

    /// <summary>
    /// Checks whether the specified idempotency key is considered valid.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to validate.</param>
    /// <param name="exception">The resulting validation exception where the idempotency key is invalid.</param>
    /// <returns><see langword="true"/> indicates that the idempotency key is valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>The idempotency key must be between 8 and 128 characters in length and consist of only letters, numbers, hyphens and underscores.</remarks>
    public static bool IsIdempotencyKeyValid([NotNullWhen(true)] string? idempotencyKey, [NotNullWhen(false)] out ValidationException? exception)
    {
        if (string.IsNullOrEmpty(idempotencyKey) || !_idempotencyKeyRegex().IsMatch(idempotencyKey))
        {
            exception = new ValidationException($"The '{HttpNames.IdempotencyKeyHeaderName}' header is invalid.")
                .WithErrorType(_errorType)
                .WithErrorCode("header-is-invalid")
                .WithDetail("The idempotency key must be between 8 and 128 characters in length and consist of only letters, numbers, hyphens and underscores.");

            return false;
        }

        exception = null;
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
        .WithDetail("An Idempotency key must be provided in the request header; it must be between 8 and 128 characters in length and consist of only letters, numbers, hyphens and underscores.");

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
        .WithDetail("The response associated with the specified idempotency key completed successfully; however, it cannot be replayed as the original response representation is no longer available.");

    [GeneratedRegex("^[A-Za-z0-9_-]{8,128}$", RegexOptions.Compiled)]
    private static partial Regex _idempotencyKeyRegex();
}