namespace CoreEx.Http;

/// <summary>
/// Provides the standard names for HTTP headers and query strings.
/// </summary>
public static class HttpNames
{
    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.Skip"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string PagingSkipQueryStringName { get; set; } = "$skip";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.Take"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string PagingTakeQueryStringName { get; set; } = "$take";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.IsCountRequested"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string PagingCountQueryStringName { get; set; } = "$count";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.Skip"/> <see cref="HttpResponseMessage.Headers"/> name.
    /// </summary>
    public static string PagingSkipHeaderName { get; set; } = "X-Paging-Skip";

    /// <summary>
    /// Gets or sets the <see cref="PagingArgs.Take"/> <see cref="HttpResponseMessage.Headers"/> name.
    /// </summary>
    public static string PagingTakeHeaderName { get; set; } = "X-Paging-Take";

    /// <summary>
    /// Gets or sets the <see cref="PagingResult.TotalCount"/> <see cref="HttpResponseMessage.Headers"/> name.
    /// </summary>
    public static string PagingTotalCountHeaderName { get; set; } = "X-Paging-Total-Count";

    /// <summary>
    /// Gets or sets the <see cref="QueryArgs.Filter"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string QueryFilterQueryStringName { get; set; } = "$filter";

    /// <summary>
    /// Gets or sets the <see cref="QueryArgs.OrderBy"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string QueryOrderByQueryStringName { get; set; } = "$orderby";

    /// <summary>
    /// Gets or sets the <see cref="QueryArgs.IncludeFields"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string IncludeFieldsQueryStringName { get; set; } = "$fields";

    /// <summary>
    /// Gets or sets the <see cref="QueryArgs.ExcludeFields"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string ExcludeFieldsQueryStringName { get; set; } = "$exclude";

    /// <summary>
    /// Gets or sets the <see cref="QueryArgs.IncludeText"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string IncludeTextQueryStringName { get; set; } = "$text";

    /// <summary>
    /// Gets or sets the <see cref="QueryArgs.IncludeInactive"/> <see cref="HttpRequestMessage.RequestUri"/> <see cref="Uri.Query"/> name.
    /// </summary>
    public static string IncludeInactiveQueryStringName { get; set; } = "$inactive";

    /// <summary>
    /// Gets or sets the <see cref="IExtendedException.ErrorType"/> problem extensions name.
    /// </summary>
    public static string ErrorTypeName { get; set; } = "errorType";

    /// <summary>
    /// Gets or sets the <see cref="IExtendedException.ErrorCode"/> problem extensions name.
    /// </summary>
    public static string ErrorCodeName { get; set; } = "errorCode";

    /// <summary>
    /// Gets or sets the tracing <see cref="Activity.Id"/> problem extensions name.
    /// </summary>
    public static string TraceIdName { get; set; } = "traceId";

    /// <summary>
    /// Gets or sets the <see cref="MessageType.Warning"/> <see cref="MessageItem"/>(s) <see cref="HttpResponseMessage.Headers"/> name.
    /// </summary>
    public static string WarningMessagesHeaderName { get; set; } = "X-Warning-Messages";

    /// <summary>
    /// Getsor sets  the <see cref="MessageType.Info"/> <see cref="MessageItem"/>(s) <see cref="HttpResponseMessage.Headers"/> name.
    /// </summary>
    public static string InfoMessagesHeaderName { get; set; } = "X-Info-Messages";

    /// <summary>
    /// Gets the JSON Merge Patch <see cref="MediaTypeNames.Application"/> name as per <see href="https://tools.ietf.org/html/rfc7396"/>.
    /// </summary>
    public const string MergePatchJsonMediaTypeName = "application/merge-patch+json";

    /// <summary>
    /// Gets the Idempotency Key <see cref="HttpRequestMessage.Headers"/> name.
    /// </summary>
    public const string IdempotencyKeyHeaderName = "Idempotency-Key";
}