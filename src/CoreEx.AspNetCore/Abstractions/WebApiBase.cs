namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Provides the base ASP.NET Core Web API capabilities to enable both MVC and HTTP support in a consistent manner.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <param name="logger">The optional <see cref="ILogger"/> for the <see cref="WebApiBase"/>.</param>
/// <param name="executionContext">The optional <see cref="CoreEx.ExecutionContext"/>.</param>
public abstract class WebApiBase(JsonSerializerOptions? jsonSerializerOptions = null, ILogger? logger = null, ExecutionContext? executionContext = null)
{
    private JsonMergePatch? _jsonMergePatch;

    /// <summary>
    /// Gets the request body error type.
    /// </summary>
    internal const string RequestBodyErrorType = "request-body";

    /// <summary>
    /// Gets the request body required text.
    /// </summary>
    internal static readonly LText RequestBodyRequiredText = new("CoreEx.AspNetCore.WebApi.RequestBodyRequired", "Request body is required.");

    /// <summary>
    /// Gets the request body invalid text.
    /// </summary>
    internal static readonly LText RequestBodyInvalidText = new("CoreEx.AspNetCore.WebApi.RequestBodyInvalid", "Request body is invalid: {0}");

    /// <summary>
    /// The configuration name to indicate whether to include exception details in the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>.
    /// </summary>
    protected const string IncludeExceptionInProblemDetailsName = "CoreEx:IncludeExceptionInProblemDetails";

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger? Logger { get; } = logger;

    /// <summary>
    /// Gets the <see cref="CoreEx.ExecutionContext"/>.
    /// </summary>
    public ExecutionContext? ExecutionContext { get; } = executionContext;

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="JsonDefaults.SerializerOptions"/>.</remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;

    /// <summary>
    /// Gets or sets the default media type for generated JSON responses.
    /// </summary>
    /// <remarks>Defaults to <c>application/json</c>. This labels serialized JSON; it does not select a serializer, change request parsing, negotiate content, or affect exception results.</remarks>
    public string DefaultJsonMediaType
    {
        get;
        set => field = ValidateJsonMediaType(value, nameof(DefaultJsonMediaType));
    } = MediaTypeNames.Application.Json;

    /// <summary>
    /// Validates a concrete response media type and its optional encoding.
    /// </summary>
    internal static string ValidateJsonMediaType(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || !MediaTypeHeaderValue.TryParse(value, out var mediaType) || mediaType.MediaType.Value?.Contains('*') != false)
            throw new ArgumentException("A valid, non-wildcard response media type is required.", parameterName);

        if (mediaType.Parameters.Count(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name.Value, "charset")) > 1
            || (mediaType.Charset.HasValue && !StringComparer.OrdinalIgnoreCase.Equals(mediaType.Charset.Value?.Trim('"'), "utf-8")))
            throw new ArgumentException("Generated JSON responses only support the UTF-8 charset.", parameterName);

        return mediaType.ToString();
    }

    /// <summary>
    /// Indicates whether to convert a <see cref="NotFoundException"/> to the default <see cref="HttpStatusCode"/> on <see cref="HttpMethods.Delete"/>.
    /// </summary>
    public bool ConvertNotfoundToDefaultStatusCodeOnDelete { get; set; } = true;

    /// <summary>
    /// Indicates whether to convert unhandled exceptions to an exception result; otherwise, allow to bubble up for middleware to handle.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/> to allow the middleware to handle unhandled exceptions in a consistent/standardized manner. The default result is <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>;
    /// a specialized WebApi can override its exception representation without enabling this flag. Cancellation attributable to the operation's token always propagates.</remarks>
    public bool ConvertUnhandledExceptionsToProblemDetails { get; set; } = false;

    /// <summary>
    /// Gets or sets the optional <see cref="JsonMergePatch"/>.
    /// </summary>
    public JsonMergePatch JsonMergePatch { get => _jsonMergePatch ??= new JsonMergePatch(new JsonMergePatchOptions(JsonSerializerOptions)); set => _jsonMergePatch = value; }

    /// <summary>
    /// Indicates whether to use absolute paths versus relative in the likes of response headers, etc.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/> indicates the use of <i>relative</i> paths.</remarks>
    public bool UseAbsolutePaths { get; set; } = false;

    /// <summary>
    /// Classifies and logs an exception and applies its transport headers before representation-specific rendering.
    /// </summary>
    internal IExtendedException? PrepareException<TWebApi>(HttpResponse response, Exception exception, bool bypassExceptionLogging)
    {
        var extendedException = exception as IExtendedException;
        if (!bypassExceptionLogging)
        {
            if (extendedException is null || !extendedException.IsError)
            {
                if (!ConvertUnhandledExceptionsToProblemDetails)
                    System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();

                extendedException = null;
                var logger = Logger ?? response.HttpContext.RequestServices.GetRequiredService<ILogger<TWebApi>>();
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError(exception, "{Error}", exception.Message);
            }
            else
            {
                var logger = Logger ?? response.HttpContext.RequestServices.GetRequiredService<ILogger<TWebApi>>();
                if (extendedException.ShouldBeLogged && logger.IsEnabled(LogLevel.Error))
                    logger.LogError(exception, "{Error}", extendedException.Message);
            }
        }

        // Validation problem responses historically omit Retry-After.
        if (extendedException is not ValidationException { Messages.Count: > 0 } && extendedException?.RetryAfter is not null)
            new WebApiHeader { RetryAfter = extendedException.RetryAfter }.ApplyTo(this, response);

        return extendedException;
    }

    /// <summary>
    /// Check the <paramref name="request"/> to ensure valid to continue.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequest"/>.</param>
    /// <param name="expectedmethods">The expected <see cref="HttpMethod"/> names.</param>
    /// <param name="memberName">The calling member name to include in any resulting <see cref="ArgumentException"/>.</param>
    protected static void CheckRequest([NotNull] HttpRequest request, string[] expectedmethods, [CallerMemberName] string? memberName = null)
    {
        request.ThrowIfNull();

        foreach (var em in expectedmethods)
        {
            if (HttpMethods.Equals(request.Method, em))
                return;
        }

        throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{string.Join(", ", expectedmethods)}' to use {memberName ?? "??"}.", nameof(request));
    }
}
