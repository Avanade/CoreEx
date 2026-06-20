namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Represents the base <see cref="WebApi{TResult}"/> options.
/// </summary>
public class WebApiOptionsBase 
{
    private QueryArgs? _queryArgs;
    private PagingArgs? _pagingArgs;
    private bool _attemptedIncludeFields;
    private bool _attemptedExcludeFields;
    private List<string>? _includeFields;
    private List<string>? _excludeFields;
    private bool? _isIncludeRelatedText;
    private bool? _isIncludeInactive;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiOptions"/> class.
    /// </summary>
    /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
    internal WebApiOptionsBase(HttpRequest httpRequest)
    {
        Request = httpRequest.ThrowIfNull();
        HasQueryString = Request.Query is not null && Request.Query.Count > 0;

        // Get the ETag from the request headers and parse accordingly.
        StringSegment? etag;
        var rth = httpRequest.GetTypedHeaders();

        if (HttpMethods.IsGet(httpRequest.Method) || HttpMethods.IsHead(httpRequest.Method))
            etag = rth.IfNoneMatch.FirstOrDefault()?.Tag;
        else if (HttpMethods.IsPut(httpRequest.Method) || HttpMethods.IsPatch(httpRequest.Method))
            etag = rth.IfMatch.FirstOrDefault()?.Tag;
        else
            etag = null;

        if (etag is not null && etag.HasValue)
            ETag = Entities.ETag.ParseETag(etag.Value);

        // Also, check whether the include text query string was specified.
        if (IsIncludeText && ExecutionContext.TryGetCurrent(out var ec))
            ec.IncludeRelatedText = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiOptions"/> class from an existing instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApiOptionsBase"/>.</param>
    public WebApiOptionsBase(WebApiOptionsBase options)
    {
        Request = options.Request;
        HasQueryString = options.HasQueryString;
        OperationType = options.OperationType;
        ETag = options.ETag;
        StatusCode = options.StatusCode;
        LocationUri = options.LocationUri;

        _queryArgs = options._queryArgs;
        _pagingArgs = options._pagingArgs;
        _attemptedIncludeFields = options._attemptedIncludeFields;
        _attemptedExcludeFields = options._attemptedExcludeFields;
        _includeFields = options._includeFields;
        _excludeFields = options._excludeFields;
        _isIncludeRelatedText = options._isIncludeRelatedText;
        _isIncludeInactive = options._isIncludeInactive;
    }

    /// <summary>
    /// Gets the originating <see cref="HttpRequest"/>.
    /// </summary>
    public HttpRequest Request { get; }

    /// <summary>
    /// Indicates whether the <see cref="Request"/> has a query string.
    /// </summary>
    public bool HasQueryString { get; }

    /// <summary>
    /// Gets the <see cref="CoreEx.OperationType"/>.
    /// </summary>
    /// <remarks>This is used to set (override) the <see cref="ExecutionContext.OperationType"/> per request.</remarks>
    public OperationType OperationType
    {
        get => field;

        internal set
        {
            field = value;
            if (ExecutionContext.TryGetCurrent(out var ec))
                ec.OperationType = value;
        }
    } = OperationType.Unspecified;

    /// <summary>
    /// Gets the entity tag that was passed; a) <c>If-None-Match</c> header where <see cref="HttpMethod.Get"/>, b) <c>If-Match</c> header where <see cref="HttpMethod.Put"/>, or c) otherwise, <see langword="null"/>.
    /// </summary>
    /// <remarks>Represents the underlying raw value; i.e. is stripped of any <c>W/"xxxx"</c> formatting.</remarks>
    public string? ETag { get; }

    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/>.
    /// </summary>
    public HttpStatusCode StatusCode { get; internal set; }

    /// <summary>
    /// Gets the alternate <see cref="HttpStatusCode"/>.
    /// </summary>
    public HttpStatusCode? AlternateStatusCode { get; internal set; }

    /// <summary>
    /// Gets the function that will return the <see cref="Uri"/> representing the location of the resource.
    /// </summary>
    public Func<Uri>? LocationUri { get; internal set; }

    /// <summary>
    /// Gets the list of <i>included</i> fields.
    /// </summary>
    /// <remarks>The <see cref="IncludeFields"/> and <see cref="ExcludeFields"/> are mutually exclusive.</remarks>
    public List<string>? IncludeFields
    {
        get
        {
            if (_includeFields is null && !_attemptedIncludeFields)
            {
                _includeFields = GetNamedQueryStrings(Request.Query, HttpNames.IncludeFieldsQueryStringName);
                _attemptedIncludeFields = true;
            }

            return _includeFields;
        }
    }

    /// <summary>
    /// Gets the list of <i>excluded</i> fields.
    /// </summary>
    /// <remarks>The <see cref="IncludeFields"/> and <see cref="ExcludeFields"/> are mutually exclusive.</remarks>
    public List<string>? ExcludeFields
    {
        get
        {
            if (_excludeFields is null && !_attemptedExcludeFields)
            {
                _excludeFields = GetNamedQueryStrings(Request.Query, HttpNames.ExcludeFieldsQueryStringName);
                _attemptedExcludeFields = true;
            }

            return _excludeFields;
        }
    }

    /// <summary>
    /// Indicates whether <see cref="HttpNames.IncludeTextQueryStringName"/> is specified within the current request to include text(s) where available.
    /// </summary>
    public bool IsIncludeText => _isIncludeRelatedText ??= (HasQueryString && ParseBoolValue(GetNamedQueryString(Request.Query, HttpNames.IncludeTextQueryStringName, "true")));

    /// <summary>
    /// Indicates whether <see cref="HttpNames.IncludeInactiveQueryStringName"/> is specified within the current request to include inactive items for the resulting item(s).
    /// </summary>
    public bool IsIncludeInactive => _isIncludeInactive ??= (HasQueryString && ParseBoolValue(GetNamedQueryString(Request.Query, HttpNames.IncludeInactiveQueryStringName, "true")));

    /// <summary>
    /// Gets the <see cref="QueryArgs"/>.
    /// </summary>
    public QueryArgs QueryArgs => _queryArgs ??= GetQueryArgs();

    /// <summary>
    /// Gets the <see cref="PagingArgs"/>.
    /// </summary>
    public PagingArgs PagingArgs => _pagingArgs ??= GetPagingArgs();

    /// <summary>
    /// Gets the <see cref="QueryArgs"/> from the <see cref="IQueryCollection"/>.
    /// </summary>
    private QueryArgs GetQueryArgs()
    {
        if (!HasQueryString)
            return new QueryArgs();

        return new QueryArgs()
        {
            Filter = GetNamedQueryString(Request.Query, HttpNames.QueryFilterQueryStringName),
            OrderBy = GetNamedQueryString(Request.Query, HttpNames.QueryOrderByQueryStringName),
            IncludeFields = IncludeFields,
            ExcludeFields = ExcludeFields,
            IsIncludeText = IsIncludeText,
            IsIncludeInactive = IsIncludeInactive
        };
    }

    /// <summary>
    /// Gets the <see cref="PagingArgs"/> from the <see cref="IQueryCollection"/>.
    /// </summary>
    private PagingArgs GetPagingArgs()
    {
        if (!HasQueryString)
            return PagingArgs.Create();

        string? skip = GetNamedQueryString(Request.Query, HttpNames.PagingSkipQueryStringName);
        string? take = GetNamedQueryString(Request.Query, HttpNames.PagingTakeQueryStringName);
        string? count = GetNamedQueryString(Request.Query, HttpNames.PagingCountQueryStringName, "true");

        if (skip is null && take is null && count is null)
            return PagingArgs.Create();

        return new PagingArgs(ParseInt32Value(skip) ?? 0, ParseInt32Value(take) ?? PagingArgs.DefaultTake, ParseBoolValue(count));
    }

    /// <summary>
    /// Parses the value as a <see cref="int"/>.
    /// </summary>
    private static int? ParseInt32Value(string? value) => int.TryParse(value, out int val) ? (val < 0 ? null : val) : null;

    /// <summary>
    /// Parses the value as a <see cref="bool"/>.
    /// </summary>
    private static bool ParseBoolValue(string? value) => bool.TryParse(value, out bool val) && val;

    /// <summary>
    /// Gets the first value for the named query string.
    /// </summary>
    private static string? GetNamedQueryString(IQueryCollection query, string name, string? emptyValue = null)
    {
        var q = query.FirstOrDefault(x => string.Compare(x.Key, name, StringComparison.OrdinalIgnoreCase) == 0);
        var val = q.Value.FirstOrDefault();
        return val is null ? null : (string.IsNullOrEmpty(val) ? emptyValue : val);
    }

    /// <summary>
    /// Gets all the named query strings and splits them by comma.
    /// </summary>
    private static List<string>? GetNamedQueryStrings(IQueryCollection query, string name)
    {
        List<string>? fields = null;
        foreach (var q in query.Where(x => string.Compare(x.Key, name, StringComparison.OrdinalIgnoreCase) == 0))
        {
            foreach (var v in q.Value)
            {
                if (v is not null)
                    (fields ??= []).AddRange(v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }

        return fields is null || fields.Count == 0 ? null : [.. fields.Distinct()];
    }

    /// <summary>
    /// Verifies the request from a <see cref="WebApiOptions"/> perspective; i.e. whether the request is valid for further processing.
    /// </summary>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>This is intended to verify the semantic request; i.e. the request content and structure, rather than syntactic correctness. For example, whether a <see cref="HttpMethods.Put"/>
    /// requires an <see cref="HttpRequestHeader.IfMatch"/> (ETag).</remarks>
    protected internal virtual Result Verify() => Result.Success;

    /// <summary>
    /// Tries to verify the request from a <see cref="WebApiOptions"/> perspective; i.e. whether the request is valid for further processing.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    internal bool IsInError(out Result result)
    {
        var r = Verify();
        result = r;
        return r.IsFailure;
    }
}