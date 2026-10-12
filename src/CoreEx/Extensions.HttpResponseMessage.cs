namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Configures validation of a successful, non-null deserialized HTTP response value.
    /// </summary>
    /// <typeparam name="T">The response value type.</typeparam>
    /// <param name="response">The response, whose lifetime remains the caller's responsibility.</param>
    /// <param name="validator">The response value validator.</param>
    /// <returns>A typed wrapper that validates when a value is consumed.</returns>
    public static ValidatedHttpResponse<T> WithValidator<T>(this HttpResponseMessage response, Validation.IValidator<T> validator) => new(response, validator);

    /// <summary>
    /// Converts the <see cref="HttpResponseMessage"/> into a <see cref="ProblemDetailsException"/> where not <see cref="HttpResponseMessage.IsSuccessStatusCode"/> and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationTokenSource"/>.</param>
    /// <returns>The corresponding <see cref="ProblemDetailsException"/> or <see langword="null"/>.</returns>
    public static async Task<ProblemDetailsException?> ToProblemDetailsAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.ThrowIfNull().IsSuccessStatusCode)
            return null;

        if (!MediaTypeNames.Application.ProblemJson.Equals(response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var pd = await JsonSerializer.DeserializeAsync<ProblemDetails>(new MemoryStream(Encoding.UTF8.GetBytes(content)), JsonDefaults.SerializerOptions, cancellationToken).ConfigureAwait(false);
            if (pd is not null)
                return new ProblemDetailsException(pd, new HttpRequestException($"{CreateMessage(response)} Problem details:{content}"));
        }
        catch (OperationCanceledException) { throw; } // Let cancellation propagate; do not treat as "not a problem details".
        catch { } // Swallow and assume not a problem details.

        return null;
    }

    /// <summary>
    /// Where the <see cref="HttpResponseMessage"/> is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, this method
    /// converts the <see cref="HttpResponseMessage"/> into a <see cref="ProblemDetailsException"/> (see <see cref="ToProblemDetailsAsync(HttpResponseMessage, CancellationToken)"/> and invokes <see cref="ProblemDetailsException.ThrowOnBusinessException"/>;
    /// otherwise, continues without error.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationTokenSource"/>.</param>
    /// <returns>The corresponding <see cref="ProblemDetailsException"/> or <see langword="null"/>.</returns>
    public static async Task<ProblemDetailsException?> ThrowOnBusinessExceptionAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var pde = await response.ToProblemDetailsAsync(cancellationToken).ConfigureAwait(false);
        pde?.ThrowOnBusinessException();
        return pde;
    }

    /// <summary>
    /// Where the <see cref="HttpResponseMessage"/> is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) will throw a corresponding exception; otherwise, continues without error.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationTokenSource"/>.</param>
    /// <remarks>Where the response is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, the content is
    /// converted into a <see cref="ProblemDetailsException"/> and returned as the error; otherwise, an <see cref="HttpRequestException"/> is returned as the error.
    /// <para>Additionally, where the <see cref="ProblemDetailsException"/> is considered a <see cref="BusinessException"/>, it is thrown as such.</para>
    /// <para>Finally, where the response is successful, not action is taken.</para></remarks>
    public static async Task ThrowOnErrorAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        var pde = await response.ThrowOnBusinessExceptionAsync(cancellationToken).ConfigureAwait(false);
        if (pde is not null)
            throw pde;

        throw new HttpRequestException(CreateMessage(response), null, response.StatusCode);
    }

    /// <summary>
    /// Creates the standard error message for a non-successful <see cref="HttpResponseMessage"/>.
    /// </summary>
    private static string CreateMessage(HttpResponseMessage response) => string.IsNullOrWhiteSpace(response.ReasonPhrase)
        ? $"Response status code does not indicate success: {(int)response.StatusCode}."
        : $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";

    /// <summary>
    /// Converts the <see cref="HttpResponseMessage"/> into a <see cref="Result"/>.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="Result"/>.</returns>
    /// <remarks>Where the response is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, the content is
    /// converted into a <see cref="ProblemDetailsException"/> and returned as the error; otherwise, an <see cref="HttpRequestException"/> is returned as the error.
    /// <para>Additionally, where the <see cref="ProblemDetailsException"/> is considered a <see cref="BusinessException"/>, it is returned as such.</para>
    /// <para>Finally, where the response is successful, a <see cref="Result.Success"/> is returned.</para></remarks>
    public static async Task<Result> ToResultAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.ThrowIfNull().IsSuccessStatusCode)
            return Result.Success;

        var pde = await response.ToProblemDetailsAsync(cancellationToken).ConfigureAwait(false);
        if (pde is not null)
            return pde.TryGetBusinessException(out var be) ? be : pde;

        return Result.Fail(new HttpRequestException(CreateMessage(response), null, response.StatusCode));
    }

    /// <summary>
    /// Converts the <see cref="HttpResponseMessage"/> into a <see cref="Result{T}"/> including the deserialized JSON response value where successful.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the response is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, the content is
    /// converted into a <see cref="ProblemDetailsException"/> and returned as the error; otherwise, an <see cref="HttpRequestException"/> is returned as the error.
    /// <para>Additionally, where the <see cref="ProblemDetailsException"/> is considered a <see cref="BusinessException"/>, it is returned as such.</para>
    /// <para>Finally, where the response is successful, a <see cref="Result{T}"/> is returned that includes the deserialized response value. Where the response content is empty or deserializes to <see langword="null"/>, an <see cref="HttpRequestException"/> is returned as the error;
    /// use <see cref="ToResultOrDefaultAsync{T}(HttpResponseMessage, JsonSerializerOptions?, CancellationToken)"/> where a <see langword="null"/> value is valid.</para></remarks>
    public static async Task<Result<T>> ToResultAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var result = await ToResultOrDefaultAsync<T>(response, jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        return result.Value is null
            ? Result.Fail(new HttpRequestException($"The response content was empty or null; a value of type '{typeof(T).Name}' was expected.", null, response.StatusCode))
            : result.Value;
    }

    /// <summary>
    /// Converts the <see cref="HttpResponseMessage"/> into a <see cref="Result{T}"/> including the deserialized JSON response value where successful.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the response is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, the content is
    /// converted into a <see cref="ProblemDetailsException"/> and returned as the error; otherwise, an <see cref="HttpRequestException"/> is returned as the error.
    /// <para>Additionally, where the <see cref="ProblemDetailsException"/> is considered a <see cref="BusinessException"/>, it is returned as such.</para>
    /// <para>Finally, where the response is successful, a <see cref="Result{T}"/> is returned that includes the deserialized response value.</para></remarks>
    public static Task<Result<T>> ToResultAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default) => ToResultAsync<T>(response, null, cancellationToken);

    /// <summary>
    /// Converts the <see cref="HttpResponseMessage"/> into a <see cref="Result{T}"/> including the deserialized JSON response value where successful; a successful response with empty or <see langword="null"/> content results in a <see langword="null"/> value.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    /// <remarks>Where the response is not successful the error handling is the same as <see cref="ToResultAsync(HttpResponseMessage, CancellationToken)"/>. Use <see cref="ToResultAsync{T}(HttpResponseMessage, JsonSerializerOptions?, CancellationToken)"/> where a value is always expected.</remarks>
    public static async Task<Result<T?>> ToResultOrDefaultAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        // Where not successful, reuse the non-generic version above to get the error details.
        if (!response.ThrowIfNull().IsSuccessStatusCode)
            return await ToResultAsync(response, cancellationToken).ConfigureAwait(false);

        // Where successful, attempt to read the content as JSON and return as the value; empty content (e.g. 204) is treated as no value as it is not valid JSON.
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return default;

        var value = await response.Content.ReadFromJsonAsync<T>(jsonSerializerOptions ?? JsonDefaults.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Converts the <see cref="HttpResponseMessage"/> into a <see cref="Result{T}"/> including the deserialized JSON response value where successful; a successful response with empty or <see langword="null"/> content results in a <see langword="null"/> value.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    /// <remarks>See <see cref="ToResultOrDefaultAsync{T}(HttpResponseMessage, JsonSerializerOptions?, CancellationToken)"/>.</remarks>
    public static Task<Result<T?>> ToResultOrDefaultAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default) => ToResultOrDefaultAsync<T>(response, null, cancellationToken);

    /// <summary>
    /// Gets the deserialized JSON response value from the <see cref="HttpResponseMessage"/> where successful; otherwise, will throw a corresponding exception.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where successful.</returns>
    /// <remarks>Where the response is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, the content is
    /// converted into a <see cref="ProblemDetailsException"/> and returned as the error; otherwise, an <see cref="HttpRequestException"/> is returned as the error.
    /// <para>Additionally, where the <see cref="ProblemDetailsException"/> is considered a <see cref="BusinessException"/>, it is thrown as such.</para>
    /// <para>Finally, where the response is successful, the deserialized response value is returned; an empty or <see langword="null"/> response content is treated as an error. Use <see cref="GetValueOrDefaultAsync{T}(HttpResponseMessage, JsonSerializerOptions?, CancellationToken)"/> where a <see langword="null"/> value is valid.</para></remarks>
    public async static Task<T> GetValueAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var result = await response.ThrowIfNull().ToResultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    /// <summary>
    /// Gets the deserialized JSON response value from the <see cref="HttpResponseMessage"/> where successful; otherwise, will throw a corresponding exception.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where successful.</returns>
    /// <remarks>Where the response is not successful (<see cref="HttpResponseMessage.IsSuccessStatusCode"/>) and the content media type is <see cref="MediaTypeNames.Application.ProblemJson"/>, the content is
    /// converted into a <see cref="ProblemDetailsException"/> and returned as the error; otherwise, an <see cref="HttpRequestException"/> is returned as the error.
    /// <para>Additionally, where the <see cref="ProblemDetailsException"/> is considered a <see cref="BusinessException"/>, it is thrown as such.</para>
    /// <para>Finally, where the response is successful, the deserialized response value is returned; an empty or <see langword="null"/> response content is treated as an error. Use <see cref="GetValueOrDefaultAsync{T}(HttpResponseMessage, CancellationToken)"/> where a <see langword="null"/> value is valid.</para></remarks>
    public static Task<T> GetValueAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default) => GetValueAsync<T>(response, null, cancellationToken);

    /// <summary>
    /// Gets the deserialized JSON response value from the <see cref="HttpResponseMessage"/> where successful, or <see langword="null"/> where the content is empty or <see langword="null"/>; otherwise, will throw a corresponding exception.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where successful.</returns>
    /// <remarks>Where not successful the error handling is the same as <see cref="GetValueAsync{T}(HttpResponseMessage, JsonSerializerOptions?, CancellationToken)"/>.</remarks>
    public async static Task<T?> GetValueOrDefaultAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        var result = await response.ThrowIfNull().ToResultOrDefaultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    /// <summary>
    /// Gets the deserialized JSON response value from the <see cref="HttpResponseMessage"/> where successful, or <see langword="null"/> where the content is empty or <see langword="null"/>; otherwise, will throw a corresponding exception.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The value where successful.</returns>
    /// <remarks>See <see cref="GetValueOrDefaultAsync{T}(HttpResponseMessage, JsonSerializerOptions?, CancellationToken)"/>.</remarks>
    public static Task<T?> GetValueOrDefaultAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default) => GetValueOrDefaultAsync<T>(response, null, cancellationToken);
}
