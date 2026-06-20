namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Provides the base ASP.NET Core Web API capabilities to enable both MVC and HTTP <typeparamref name="TResult"/> support in a consistent manner.
/// </summary>
/// <typeparam name="TResult">The ASP.NET Core result <see cref="Type"/>.</typeparam>
/// <param name="invoker">The <see cref="WebApiInvoker{TResult}"/>.</param>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/> for the <see cref="WebApi{TResult}"/>.</param>
/// <param name="executionContext">The optional <see cref="ExecutionContext"/>.</param>
/// <remarks>The <see cref="HttpMethods.Get"/> methods within can also be used for a <see cref="HttpMethods.Head"/> as it is essentially the same operation without a corresponding response; this distinction is handled internally.</remarks>
public abstract partial class WebApi<TResult>(WebApiInvoker<TResult> invoker, JsonSerializerOptions? jsonSerializerOptions = null, ILogger<WebApi<TResult>>? logger = null, ExecutionContext? executionContext = null)
    : WebApiBase(jsonSerializerOptions, logger, executionContext)
{
    private const string _requestBodyErrorType = "request-body";
    private static readonly LText _requestBodyRequiredText = new("CoreEx.AspNetCore.WebApi.RequestBodyRequired", "Request body is required.");
    private static readonly LText _requestBodyInvalidText = new("CoreEx.AspNetCore.WebApi.RequestBodyInvalid", "Request body is invalid: {0}");

    private readonly WebApiInvoker<TResult> _invoker = invoker.ThrowIfNull();

    /// <summary>
    /// Invokes the <paramref name="function"/> asynchronously performing standardized execution.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <param name="memberName">The calling member name (uses <see cref="CallerMemberNameAttribute"/> to default).</param>
    protected async Task<TResult> InvokeAsync(HttpRequest request, Func<CancellationToken, Task<TResult>> function, CancellationToken cancellationToken, [CallerMemberName] string? memberName = null) => await _invoker.InvokeAsync(this, async (_, cancellationToken) =>
    {
        try
        {
            var result = await function(cancellationToken).ConfigureAwait(false);
            if (ExecutionContext.TryGetCurrent(out var ec))
                ExecutionContextMiddleware.AddMessagesHeader(request.HttpContext, ec);

            return result;
        }
        catch (Exception ex) when (ex is IExtendedException eex && eex.IsError)
        {
            return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ex });
        }
        catch (Exception ex) when (ConvertUnhandledExceptionsToProblemDetails)
        {
            return CreateResult(new WebApiResult<TResult>(request.HttpContext.Response) { Exception = ex });
        }
    }, cancellationToken, memberName).ConfigureAwait(false);

    /// <summary>
    /// Creates the corresponding <see cref="WebApi{TResult}"/> for the <paramref name="value"/>, etc.
    /// </summary>
    private WebApiResult<TResult> CreateContentForValue<T>(WebApiOptionsBase options, T value, WebApiPagingResult? paging = null, MessageItemCollection? messages = null)
    {
        options.ThrowIfNull();

        // Where null then use the alternate status code.
        if (value is null)
        {
            return options.AlternateStatusCode.HasValue 
                ? new WebApiResult<TResult>(options.Request.HttpContext.Response) { StatusCode = options.AlternateStatusCode.Value, Exception = OnConvertAlternateStatusCodeToException(options.AlternateStatusCode.Value) } 
                : throw new InvalidOperationException("Function has not returned a result; no AlternateStatusCode has been configured to return.");
        }

        // Where already a TResult then return as-is.
        if (value is TResult tr)
            return new WebApiResult<TResult>(options.Request.HttpContext.Response) { Result = tr };

        // Special case when is railway-oriented IResult, as it is the underlying Value or Exception that is to be processed.
        if (value is Results.Abstractions.IResult r)
        {
            if (r.IsFailure)
                return new WebApiResult<TResult>(options.Request.HttpContext.Response) { Exception = r.Error! };
            else
                return CreateContentForValue(options, r.Value, paging, messages);
        }

        // Special case when IItemsResult, as it is the Items only that is to be serialized and returned.
        if (value is IItemsResult itemsResult)
        {
            var wpr = itemsResult.Paging is null ? null : new WebApiPagingResult(itemsResult.Paging, itemsResult.Items is null ? 0 : itemsResult.GetItemsCount());
            return CreateContentForValue(options, itemsResult.Items ?? Array.Empty<object?>(), wpr, messages);
        }

        // Special case when IValueResult, as it is the underlying Value and StatusCode that is to be serialized and returned.
        if (value is IValueResult valueResult)
        {
            if (valueResult.StatusCode.HasValue)
                options.StatusCode = valueResult.StatusCode.Value;

            return CreateContentForValue(options, valueResult.Value, paging, messages);
        }

        // Where there is mutable etag support and it is null (assumes auto-generation) then generate from the full value JSON contents as the baseline value.
        if (value is IETag etag && etag.ETag is null)
            etag.ETag = ETag.Generate(value, JsonSerializerOptions);

        // Serialize the value and perform any field selection/filtering as per the request options where specified.
        string json;
        if (options.IncludeFields is not null && options.IncludeFields.Count > 0)
            JsonFilter.TryFilter(value, options.IncludeFields, out json, JsonFilterOption.Include, JsonSerializerOptions);
        else if (options.ExcludeFields is not null && options.ExcludeFields.Count > 0)
            JsonFilter.TryFilter(value, options.ExcludeFields, out json, JsonFilterOption.Exclude, JsonSerializerOptions);
        else
            json = JsonSerializer.Serialize(value, JsonSerializerOptions);

        // Determine final etag - where the query string is provided this may affect the ETag and should be included in the hash as such; i.e. paging, filtering, fields, etc.
        var getag = options.HasQueryString
            ? Entities.ETag.Generate(json, options.Request.QueryString.ToString())
            : value is IReadOnlyETag vetag && vetag.ETag is not null ? vetag.ETag : ETag.Generate(json);

        // Where the request is a GET or HEAD and the ETag matches then return a 304 Not Modified.
        if (options.ETag is not null && (HttpMethods.IsGet(options.Request.Method) || HttpMethods.IsHead(options.Request.Method)) && options.ETag == getag)
            return new WebApiResult<TResult>(options.Request.HttpContext.Response)
            {
                StatusCode = HttpStatusCode.NotModified,
                Headers = new WebApiHeader { ETag = getag }
            };

        // Create the location value where applicable.
        var location = options is IWebApiResponseOptions lro ? lro.CreateLocationUri(value) : null;

        // Handle the HEAD request where no content is returned.
        if (HttpMethods.IsHead(options.Request.Method))
            return new WebApiResult<TResult>(options.Request.HttpContext.Response)
            {
                StatusCode = options.StatusCode,
                Headers = new WebApiHeader { ETag = getag, Location = location }
            };

        // All others should return content.
        return new WebApiResult<TResult>(options.Request.HttpContext.Response)
        {
            Content = json,
            ContentType = MediaTypeNames.Application.Json,
            StatusCode = options.StatusCode,
            Headers = new WebApiHeader
            {
                ETag = getag,
                Location = location,
                PagingResult = paging
            }
        };
    }

    /// <summary>
    /// Creates a <typeparamref name="TResult"/> for the specified <see cref="WebApiResult{TResult}"/>.
    /// </summary>
    /// <param name="result">The <see cref="WebApiResult{TResult}"/>.</param>
    /// <returns>The content <typeparamref name="TResult"/>.</returns>
    internal abstract TResult CreateResult(WebApiResult<TResult> result);

    /// <summary>
    /// Gets the <typeparamref name="TRequest"/> value from the <see cref="HttpRequest"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request JSON content value <see cref="Type"/>.</typeparam>
    /// <param name="request">The <see cref="HttpResponse"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    protected async Task<Result<TRequest?>> GetRequestValueAsync<TRequest>(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength == 0)
            return new ValidationException(_requestBodyRequiredText).WithErrorType(_requestBodyErrorType);

        try
        {
            return await request.ReadFromJsonAsync<TRequest>(JsonSerializerOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new ValidationException(_requestBodyInvalidText.WithArgs(ex.Message)).WithErrorType(_requestBodyErrorType);
        }
    }

    /// <summary>
    /// Creates the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> extensions for the <paramref name="exception"/>.
    /// </summary>
    /// <param name="exception">The <see cref="IExtendedException"/>.</param>
    /// <returns>The <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails.Extensions"/>.</returns>
    protected IDictionary<string, object?> CreateProblemDetailsExtensions(IExtendedException? exception)
    {
        var ext = new Dictionary<string, object?>();

        if (exception is not null)
        {
            if (exception.ErrorType is not null)
                ext.Add(HttpNames.ErrorTypeName, exception.ErrorType);

            if (exception.ErrorCode is not null)
                ext.Add(HttpNames.ErrorCodeName, exception.ErrorCode);
        }

        if (Activity.Current is not null)
            ext.Add(HttpNames.TraceIdName, Activity.Current.Id);

        return ext;
    }

    /// <summary>
    /// Provides the ability to convert an alternate <see cref="HttpStatusCode"/> to an <see cref="Exception"/> (where required).
    /// </summary>
    /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
    /// <returns>The resulting <see cref="Exception"/> where applicable; otherwise, <see langword="null"/> indicating no conversion required.</returns>
    /// <remarks>Attempts to convert all known error (>= 400) <paramref name="statusCode"/> values to their <see cref="IExtendedException"/> equivalents; otherwise, defaults to an <see cref="UnexpectedInternalException"/>.</remarks>
    protected virtual Exception? OnConvertAlternateStatusCodeToException(HttpStatusCode statusCode)
    {
        if ((int)statusCode < 400)
            return null;

        return (int)statusCode switch
        {
            (int)HttpStatusCode.Unauthorized => new AuthenticationException(),
            (int)HttpStatusCode.Forbidden => new AuthorizationException(),
            (int)HttpStatusCode.BadRequest => new ValidationException(),
            (int)HttpStatusCode.PreconditionFailed => new ConcurrencyException(),
            (int)HttpStatusCode.Conflict => new ConflictException(),
            (int)HttpStatusCode.NotFound => new NotFoundException(),
            (int)HttpStatusCode.ServiceUnavailable => new TransientException(),
            _ => new UnexpectedInternalException { StatusCode = statusCode },
        };
    }
}