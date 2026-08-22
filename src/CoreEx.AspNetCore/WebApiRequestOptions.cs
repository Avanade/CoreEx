namespace CoreEx.AspNetCore;

/// <summary>
/// Represents the <see cref="WebApi{TResult}"/> request options.
/// </summary>
/// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
public sealed class WebApiRequestOptions<TRequest> : WebApiOptionsBase, IWebApiRequestOptions<TRequest>
{
    private TRequest? _valueOrDefault;
    private bool _hasBeenCleaned;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiRequestOptions{T}"/> class.
    /// </summary>
    /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The deserialized request value.</param>
    public WebApiRequestOptions(HttpRequest httpRequest, TRequest? value) : base(httpRequest)
    {
        _valueOrDefault = value;

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
        _valueOrDefault = value;

        // Override the ETag where specified as a request IF-MATCH header.
        if (value is not null && ETag is not null && value is IETag etag)
            etag.ETag = ETag;
    }

    /// <inheritdoc/>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool AutoCleanValue { get; set; } = true;

    /// <inheritdoc/>
    public TRequest? ValueOrDefault
    {
        get
        {
            if (AutoCleanValue && !_hasBeenCleaned)
            {
                _valueOrDefault = Metadata.RuntimeMetadata.Clean(_valueOrDefault, CleanArgs.Default);
                _hasBeenCleaned = true;
            }

            return _valueOrDefault;
        }
    }

    /// <inheritdoc/>
    [NotNull]
    public TRequest Value => (EqualityComparer<TRequest?>.Default.Equals(ValueOrDefault, default!))
        ? throw new ValidationException(WebApiBase.RequestBodyRequiredText).WithErrorType(WebApiBase.RequestBodyErrorType)
        : ValueOrDefault!;

    /// <inheritdoc/>
    protected internal override Result Verify() => VerifyRequest(this, ValueOrDefault).Then(() => base.Verify());

    /// <summary>
    /// Enables standard verification of the <see cref="IWebApiRequestOptions{TRequest}.ValueOrDefault"/>, such as ensuring an ETag is provided for PUT and PATCH requests.
    /// </summary>
    /// <typeparam name="TOptions">The <see cref="WebApiOptionsBase"/> <see cref="Type"/>.</typeparam>
    /// <param name="options">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="value">The request value.</param>
    /// <returns>The <see cref="Result"/> of the verification.</returns>
    /// <remarks>POST is intentionally excluded: it typically represents creation with no prior state to match against. Where a specific POST (or DELETE) operation is conditional on a
    /// related resource's state (e.g. adding an item to a booking that must not have changed since it was read), the request's <see cref="WebApiOptionsBase.ETag"/> is still captured from
    /// the <c>If-Match</c> header (see the base constructor) and stamped onto the value where it implements <see cref="IETag"/>. Call <see cref="AspNetCoreExtensions.WithIfMatchRequired{TRequestOptions}(TRequestOptions)"/>
    /// from within the handler — before any state-changing logic — to assert it immediately and fail with a <see cref="ConcurrencyException"/> where required, without any change to
    /// this shared verification path.</remarks>
    internal static Result VerifyRequest<TOptions>(TOptions options, TRequest? value) where TOptions : WebApiOptionsBase, IWebApiRequestOptions<TRequest>
    {
        if (HttpMethods.IsPut(options.Request.Method) || HttpMethods.IsPatch(options.Request.Method))
        {
            if (value is IETag etag && etag.ETag is null)
                return Result.Fail(new ConcurrencyException(ConcurrencyMessage).WithStatusCode(HttpStatusCode.PreconditionRequired));
        }

        return Result.Success;
    }
}
