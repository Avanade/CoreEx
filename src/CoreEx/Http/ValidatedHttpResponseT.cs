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
    private Dictionary<HttpStatusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>>>? _statusResults;

    /// <summary>
    /// Configures a <see cref="Result"/> to return when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The <see cref="Result"/> to return.</param>
    /// <returns>The <see cref="ValidatedHttpResponse{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpResponse<T> WithStatusResult(HttpStatusCode statusCode, Result result)
    {
        _statusResults ??= [];
        _statusResults[statusCode] = (_, _) => Task.FromResult(result);
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The function returning the <see cref="Result"/>.</param>
    /// <returns>The <see cref="ValidatedHttpResponse{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpResponse<T> WithStatusResult(HttpStatusCode statusCode, Func<Result> result)
    {
        result.ThrowIfNull();
        _statusResults ??= [];
        _statusResults[statusCode] = (_, _) => Task.FromResult(result());
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The function returning the <see cref="Result"/> for the response.</param>
    /// <returns>The <see cref="ValidatedHttpResponse{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpResponse<T> WithStatusResult(HttpStatusCode statusCode, Func<HttpResponseMessage, Result> result)
    {
        result.ThrowIfNull();
        _statusResults ??= [];
        _statusResults[statusCode] = (response, _) => Task.FromResult(result(response));
        return this;
    }

    /// <summary>
    /// Configures an asynchronous <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The asynchronous function returning the <see cref="Result"/> for the response.</param>
    /// <returns>The <see cref="ValidatedHttpResponse{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpResponse<T> WithStatusResult(HttpStatusCode statusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>> result)
    {
        result.ThrowIfNull();
        _statusResults ??= [];
        _statusResults[statusCode] = result;
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> to return when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Result)"/>.
    /// </summary>
    public ValidatedHttpResponse<T> WithErrorStatus(HttpStatusCode statusCode, Result result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{Result})"/>.
    /// </summary>
    public ValidatedHttpResponse<T> WithErrorStatus(HttpStatusCode statusCode, Func<Result> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{HttpResponseMessage, Result})"/>.
    /// </summary>
    public ValidatedHttpResponse<T> WithErrorStatus(HttpStatusCode statusCode, Func<HttpResponseMessage, Result> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures an asynchronous <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{HttpResponseMessage, CancellationToken, Task{Result}})"/>.
    /// </summary>
    public ValidatedHttpResponse<T> WithErrorStatus(HttpStatusCode statusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Attempts to get a registered custom <see cref="Result"/> for the response's status code.
    /// </summary>
    private async Task<Result?> TryGetStatusResultAsync(CancellationToken cancellationToken)
    {
        if (_statusResults is not null && _statusResults.TryGetValue(_response.StatusCode, out var handler))
            return await handler(_response, cancellationToken).ConfigureAwait(false);

        return null;
    }

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
        if (await TryGetStatusResultAsync(cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<T>();

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
        if (await TryGetStatusResultAsync(cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<T?>();

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
