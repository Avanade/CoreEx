namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Adds the <see cref="HttpRequestMessage"/> <see cref="HttpRequestHeaders.IfMatch"/> with the specified <paramref name="etag"/> (where no <see langword="null"/>).
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="etag">The entity tag value.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithIfMatch(this HttpRequestMessage request, string? etag)
    {
        request.ThrowIfNull();

        if (etag is not null)
            request.Headers.IfMatch.Add(new EntityTagHeaderValue(ETag.FormatETag(etag)));

        return request;
    }

    /// <summary>
    /// Adds the <see cref="HttpRequestMessage"/> <see cref="HttpRequestHeaders.IfNoneMatch"/> with the specified <paramref name="etag"/> (where no <see langword="null"/>).
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="etag">The entity tag value.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithIfNoneMatch(this HttpRequestMessage request, string? etag)
    {
        request.ThrowIfNull();

        if (etag is not null)
            request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(ETag.FormatETag(etag)));

        return request;
    }

    /// <summary>
    /// Sets the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.Content"/> <see cref="HttpContent.Headers"/> <see cref="HttpContentHeaders.ContentType"/> to <see cref="HttpNames.MergePatchJsonMediaTypeName"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithMergePatchJsonContentType(this HttpRequestMessage request)
    {
        request.ThrowIfNull().Content?.Headers.ContentType = new MediaTypeHeaderValue(HttpNames.MergePatchJsonMediaTypeName);
        return request;
    }

    /// <summary>
    /// Adds the <see cref="HttpRequestMessage"/> <see cref="HttpNames.IdempotencyKeyHeaderName"/> with the specified <paramref name="idempotencyKey"/> or allow to default .
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="idempotencyKey">The idempotency key.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Where no <paramref name="idempotencyKey"/> is specified, a new <see cref="Guid"/> is generated and used.</remarks>
    public static HttpRequestMessage WithIdempotencyKey(this HttpRequestMessage request, string? idempotencyKey = null)
    {
        request.ThrowIfNull().Headers.Add(HttpNames.IdempotencyKeyHeaderName, idempotencyKey ?? Guid.NewGuid().ToString());
        return request;
    }

    /// <summary>
    /// Adds <see cref="PagingArgs"/> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.RequestUri"/> by adding to the <see cref="Uri.Query"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithPaging(this HttpRequestMessage request, PagingArgs? paging)
        => request.ThrowIfNull().WithPaging(paging?.Skip, paging?.Take, paging?.IsCountRequested ?? false);

    /// <summary>
    /// Adds <see cref="PagingArgs"/> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.RequestUri"/> by adding to the <see cref="Uri.Query"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
    /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
    /// <param name="count">Indicates whether to get the total count when performing the underlying query</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithPaging(this HttpRequestMessage request, long? skip = null, long? take = null, bool count = false)
        => AddQuery(request, qb => qb
            .AddQuery(HttpNames.PagingSkipQueryStringName, skip?.ToString())
            .AddQuery(HttpNames.PagingTakeQueryStringName, take?.ToString())
            .AddQuery(HttpNames.PagingCountQueryStringName, count ? "true" : null));

    /// <summary>
    /// Adds <see cref="QueryArgs"/> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.RequestUri"/> by adding to the <see cref="Uri.Query"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="query">The <see cref="QueryArgs"/>.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithQuery(this HttpRequestMessage request, QueryArgs? query)
        => WithQuery(request, query?.Filter, query?.OrderBy, query?.IncludeFields, query?.ExcludeFields);

    /// <summary>
    /// Adds <see cref="QueryArgs"/> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.RequestUri"/> by adding to the <see cref="Uri.Query"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="filter">The basic dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
    /// <param name="orderBy">The basic dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
    /// <param name="include">The list of <b>included</b> fields.</param>
    /// <param name="exclude">The list of <b>excluded</b> fields.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithQuery(this HttpRequestMessage request, string? filter = null, string? orderBy = null, IEnumerable<string>? include = null, IEnumerable<string>? exclude = null)
        => AddQuery(request, qb => qb
            .AddQuery(HttpNames.QueryFilterQueryStringName, filter)
            .AddQuery(HttpNames.QueryOrderByQueryStringName, orderBy)
            .AddQuery(HttpNames.IncludeFieldsQueryStringName, include is null ? null : string.Join(',', include.Select(i => i?.Trim()).Where(i => !string.IsNullOrEmpty(i))))
            .AddQuery(HttpNames.ExcludeFieldsQueryStringName, exclude is null ? null : string.Join(',', exclude.Select(e => e?.Trim()).Where(e => !string.IsNullOrEmpty(e)))));

    /// <summary>
    /// Adds <see cref="QueryArgs.IncludeFields"/> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.RequestUri"/> by adding to the <see cref="Uri.Query"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="fields">The list of included fields.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithFields(this HttpRequestMessage request, params IEnumerable<string> fields) => WithQuery(request, include: fields);

    /// <summary>
    /// Adds <see cref="QueryArgs.ExcludeFields"/> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.RequestUri"/> by adding to the <see cref="Uri.Query"/>.
    /// </summary>
    /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
    /// <param name="fields">The list of excluded fields.</param>
    /// <returns>The <paramref name="request"/> to support fluent-style method-chaining.</returns>
    public static HttpRequestMessage WithoutFields(this HttpRequestMessage request, params IEnumerable<string> fields) => WithQuery(request, exclude: fields);

    /// <summary>
    /// Adds URI query parameters to the <see cref="Uri.Query"/>.
    /// </summary>
    private static HttpRequestMessage AddQuery(HttpRequestMessage request, Action<StringBuilder> queryBuilder)
    {
        request.ThrowIfNull();

        var builder = new UriBuilder(request.RequestUri!);
        var sb = new StringBuilder(builder.Query);

        queryBuilder(sb);

        builder.Query = sb.ToString();
        request.RequestUri = builder.Uri;

        return request;

    }

    /// <summary>
    /// Adds a URI query parameter to the <see cref="StringBuilder"/>.
    /// </summary>
    private static StringBuilder AddQuery(this StringBuilder sb, string name, string? value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value))
            return sb;

        if (sb.Length > 0)
            sb.Append('&');

        sb.Append($"{name}={value}");
        return sb;
    }
}