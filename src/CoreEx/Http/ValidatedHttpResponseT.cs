namespace CoreEx.Http;

/// <summary>
/// Enables opt-in validation when consuming a deserialized HTTP response value.
/// </summary>
/// <typeparam name="T">The response value type.</typeparam>
/// <remarks>This wrapper does not own the response. Validation failures are dependency failures represented by
/// <see cref="HttpRequestException"/>, not caller validation errors. Successful validation returns the validation result's value.</remarks>
/// <param name="response">The response.</param>
/// <param name="validator">The response value validator.</param>
public sealed class ValidatedHttpResponse<T>(HttpResponseMessage response, Validation.IValidator<T> validator)
{
    private readonly HttpResponseMessage _response = response.ThrowIfNull();
    private readonly Validation.IValidator<T> _validator = validator.ThrowIfNull();

    /// <summary>
    /// Gets a result containing the validated value; missing content or a null validated value is a failure.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public Task<Result<T>> ToResultAsync(CancellationToken cancellationToken = default) => ToResultAsync(null, cancellationToken);

    /// <summary>
    /// Gets a result containing the validated value; missing content or a null validated value is a failure.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public async Task<Result<T>> ToResultAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var result = await _response.ToResultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        var validated = await ValidateAsync(result.AsResult<T, T?>(), true, cancellationToken).ConfigureAwait(false);
        return validated.AsResult<T?, T>();
    }

    /// <summary>
    /// Gets a result containing the validated value, allowing null; absent response values are not validated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public Task<Result<T?>> ToResultOrDefaultAsync(CancellationToken cancellationToken = default) => ToResultOrDefaultAsync(null, cancellationToken);

    /// <summary>
    /// Gets a result containing the validated value, allowing null; absent response values are not validated.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public async Task<Result<T?>> ToResultOrDefaultAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var result = await _response.ToResultOrDefaultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        return await ValidateAsync(result, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the validated value, throwing on failure, including missing content or a null validated value.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value.</returns>
    public Task<T> GetValueAsync(CancellationToken cancellationToken = default) => GetValueAsync(null, cancellationToken);

    /// <summary>
    /// Gets the validated value, throwing on failure, including missing content or a null validated value.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value.</returns>
    public async Task<T> GetValueAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
        => (await ToResultAsync(jsonSerializerOptions, cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Gets the validated value, allowing null and throwing on failure; absent response values are not validated.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value or null.</returns>
    public Task<T?> GetValueOrDefaultAsync(CancellationToken cancellationToken = default) => GetValueOrDefaultAsync(null, cancellationToken);

    /// <summary>
    /// Gets the validated value, allowing null and throwing on failure; absent response values are not validated.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value or null.</returns>
    public async Task<T?> GetValueOrDefaultAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
        => (await ToResultOrDefaultAsync(jsonSerializerOptions, cancellationToken).ConfigureAwait(false)).Value;

    /// <summary>
    /// Validates a successfully deserialized non-null value and classifies reported errors as dependency failures.
    /// </summary>
    private async Task<Result<T?>> ValidateAsync(Result<T?> result, bool required, CancellationToken cancellationToken)
    {
        if (result.IsFailure || result.Value is null)
            return result;

        var validation = await _validator.ValidateAsync(result.Value, cancellationToken).ConfigureAwait(false);
        if (validation.HasErrors)
        {
            var error = validation.ToException() ?? new ValidationException(validation.Messages ?? []);
            return Result.Fail(new HttpRequestException($"The response value of type '{typeof(T).Name}' failed validation.", error, _response.StatusCode));
        }

        if (required && validation.Value is null)
            return Result.Fail(new HttpRequestException($"Validation returned null; a response value of type '{typeof(T).Name}' was expected.", null, _response.StatusCode));

        return Result<T?>.Ok(validation.Value);
    }
}
