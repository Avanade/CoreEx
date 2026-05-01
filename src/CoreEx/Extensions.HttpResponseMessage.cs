namespace CoreEx;

public static partial class Extensions
{
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
    /// <para>Finally, where the response is successful, a <see cref="Result{T}"/> is returned that includes the deserialized response value.</para></remarks>
    public static async Task<Result<T?>> ToResultAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
    {
        // Where not successful, reuse the non-generic version above to get the error details.
        if (!response.ThrowIfNull().IsSuccessStatusCode)
            return await ToResultAsync(response, cancellationToken).ConfigureAwait(false);

        // Where successful, attempt to read the content as JSON and return as the value.
        var value = await response.Content.ReadFromJsonAsync<T>(jsonSerializerOptions ?? JsonDefaults.SerializerOptions, cancellationToken).ConfigureAwait(false);
        return value;
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
    public static Task<Result<T?>> ToResultAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default) => ToResultAsync<T>(response, null, cancellationToken);

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
    /// <para>Finally, where the response is successful, the deserialized response value is returned.</para></remarks>
    public async static Task<T?> GetValueAsync<T>(this HttpResponseMessage response, JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
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
    /// <para>Finally, where the response is successful, the deserialized response value is returned.</para></remarks>
    public static Task<T?> GetValueAsync<T>(this HttpResponseMessage response, CancellationToken cancellationToken = default) => GetValueAsync<T>(response, null, cancellationToken);
}