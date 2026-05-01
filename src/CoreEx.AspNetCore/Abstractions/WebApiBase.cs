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
    /// Indicates whether to convert a <see cref="NotFoundException"/> to the default <see cref="HttpStatusCode"/> on <see cref="HttpMethods.Delete"/>.
    /// </summary>
    public bool ConvertNotfoundToDefaultStatusCodeOnDelete { get; } = true;

    /// <summary>
    /// Indicates whether to convert unhandled exceptions to <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/>; otherwise, allow to bubble up for middleware to handle.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/> to allow the middleware to handle unhandled exceptions in a consistent/standardized manner.</remarks>
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