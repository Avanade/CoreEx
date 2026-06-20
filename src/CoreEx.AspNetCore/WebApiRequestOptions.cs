namespace CoreEx.AspNetCore;

/// <summary>
/// Represents the <see cref="WebApi{TResult}"/> request options.
/// </summary>
/// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
public sealed class WebApiRequestOptions<TRequest> : WebApiOptionsBase, IWebApiRequestOptions<TRequest>
{
    private static readonly LText _concurrencyMessage = new($"{typeof(WebApiOptionsBase).FullName}.IfMatchRequired" , "A concurrency error occurred; an ETag is required either as an IF-MATCH header (preferred) or specified within the request body (where supported).");

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiRequestOptions{T}"/> class.
    /// </summary>
    /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The deserialized request value.</param>
    public WebApiRequestOptions(HttpRequest httpRequest, TRequest? value) : base(httpRequest)
    {
        ValueOrDefault = value;

        // Override the ETag where specified as a request IF-MATCH header.
        if (value is not null && ETag is not null && value is IETag etag)
            etag.ETag = ETag;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiRequestOptions{T}"/> class from an existing instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="value">The deserialized request value.</param>
    public WebApiRequestOptions(WebApiOptionsBase options, TRequest? value) : base(options)
    {
        ValueOrDefault = value;

        // Override the ETag where specified as a request IF-MATCH header.
        if (value is not null && ETag is not null && value is IETag etag)
            etag.ETag = ETag;
    }

    /// <inheritdoc/>
    public TRequest? ValueOrDefault { get; }

    /// <inheritdoc/>
    [NotNull]
    public TRequest Value => ValueOrDefault.Required();

    /// <inheritdoc/>
    protected internal override Result Verify() => VerifyRequest(this, ValueOrDefault).Then(() => base.Verify());

    /// <summary>
    /// Enables standard verification of the <see cref="IWebApiRequestOptions{TRequest}.ValueOrDefault"/>, such as ensuring an ETag is provided for PUT and PATCH requests.
    /// </summary>
    /// <typeparam name="TOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="options">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="value">The request value.</param>
    /// <returns>The <see cref="Result"/> of the verification.</returns>
    internal static Result VerifyRequest<TOptions>(TOptions options, TRequest? value) where TOptions : WebApiOptionsBase, IWebApiRequestOptions<TRequest>
    {
        if (HttpMethods.IsPut(options.Request.Method) || HttpMethods.IsPatch(options.Request.Method))
        {
            if (value is IETag etag && etag.ETag is null)
                return Result.Fail(new ConcurrencyException(_concurrencyMessage).WithStatusCode(HttpStatusCode.PreconditionRequired));
        }

        return Result.Success;
    }
}