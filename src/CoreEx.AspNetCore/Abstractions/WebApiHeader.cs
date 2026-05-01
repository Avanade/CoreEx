namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Provides the standard <see cref="HttpResponse"/> headers specification and corresponding <see cref="ApplyTo"/>.
/// </summary>
internal readonly struct WebApiHeader
{
    /// <summary>
    /// Gets or sets the <see cref="IHeaderDictionary"/> for additional ad-hoc <see cref="HttpResponse"/> headers.
    /// </summary>
    public IHeaderDictionary? Headers { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.
    /// </summary>
    public Uri? Location { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="TimeSpan"/> for the <see cref="System.Net.Http.Headers.RetryConditionHeaderValue"/>.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="IReadOnlyETag.ETag"/> value.
    /// </summary>
    public string? ETag { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="WebApiPagingResult"/>.
    /// </summary>
    public WebApiPagingResult? PagingResult { get; init; }

    /// <summary>
    /// Applies the <see cref="WebApiHeader"/> to the <see cref="HttpResponse"/>.
    /// </summary>
    /// <param name="webApi">The <see cref="WebApiBase"/>.</param>
    /// <param name="httpResponse">The <see cref="HttpResponse"/>.</param>
    public void ApplyTo(WebApiBase webApi, HttpResponse httpResponse)
    {
        var headers = httpResponse.GetTypedHeaders();
        if (ETag is not null)
            headers.ETag = new EntityTagHeaderValue(Entities.ETag.FormatETag(ETag), true);

        if (Location is not null)
            headers.Location = Location;

        if (RetryAfter.HasValue)
            httpResponse.Headers.Append(HeaderNames.RetryAfter, new System.Net.Http.Headers.RetryConditionHeaderValue(RetryAfter.Value).ToString());

        if (Headers is not null && Headers.Count > 0)
        {
            foreach (var header in Headers)
            {
                httpResponse.Headers.Append(header.Key, header.Value);
            }
        }

        if (PagingResult is not null)
        {
            httpResponse.Headers.Append(HttpNames.PagingSkipHeaderName, PagingResult.Skip.ToString());
            httpResponse.Headers.Append(HttpNames.PagingTakeHeaderName, PagingResult.Take.ToString());
            if (PagingResult.TotalCount.HasValue)
                httpResponse.Headers.Append(HttpNames.PagingTotalCountHeaderName, PagingResult.TotalCount.Value.ToString());

            ApplyPagingPrevAndNextLinks(webApi, httpResponse);
        }
    }

    /// <summary>
    /// Applies the previous and next links to the <see cref="HttpResponse"/>.
    /// </summary>
    private void ApplyPagingPrevAndNextLinks(WebApiBase webApi, HttpResponse httpResponse)
    {
        // Get the orginating URI without query string.
        var url = webApi.UseAbsolutePaths
            ? UriHelper.BuildAbsolute(httpResponse.HttpContext.Request.Scheme, httpResponse.HttpContext.Request.Host, httpResponse.HttpContext.Request.PathBase, httpResponse.HttpContext.Request.Path)
            : UriHelper.BuildRelative(httpResponse.HttpContext.Request.PathBase, httpResponse.HttpContext.Request.Path);

        QueryString qs;
        PagingArgs? pa = PagingResult?.GetPreviousPage();
        if (pa is not null)
        {
            qs = RebuildPagingLinkQueryString(httpResponse, pa);
            httpResponse.Headers.Append("Link", $"<{url}{qs}>; rel=\"prev\"");
        }

        pa = PagingResult?.GetNextPage();
        if (pa is not null)
        {
            qs = RebuildPagingLinkQueryString(httpResponse, pa);
            httpResponse.Headers.Append("Link", $"<{url}{qs}>; rel=\"next\"");
        }
    }

    /// <summary>
    /// Rebuild the originating request query string with the updated paging args.
    /// </summary>
    private static QueryString RebuildPagingLinkQueryString(HttpResponse httpResponse, PagingArgs paging)
    {
        var ps = false;
        var pt = false;
        var qs = new QueryString();

        foreach (var item in httpResponse.HttpContext.Request.Query)
        {
            if (item.Key.Equals(HttpNames.PagingSkipQueryStringName, StringComparison.OrdinalIgnoreCase))
            {
                ps = true;
                qs = qs.Add(HttpNames.PagingSkipQueryStringName, paging.Skip.ToString("D"));
            }
            else if (item.Key.Equals(HttpNames.PagingTakeQueryStringName, StringComparison.OrdinalIgnoreCase))
            {
                pt = true;
                qs = qs.Add(HttpNames.PagingTakeQueryStringName, paging.Take.ToString("D"));
            }
            else
            {
                if (item.Value.Count == 0)
                    qs = qs.Add(new QueryString($"?{item.Key}"));
                else
                {
                    foreach (var value in item.Value)
                    {
                        if (string.IsNullOrEmpty(value))
                            qs = qs.Add(new QueryString($"?{item.Key}"));
                        else
                            qs = qs.Add(item.Key, value);
                    }
                }
            }
        }

        if (!ps)
            qs = qs.Add(HttpNames.PagingSkipQueryStringName, paging.Skip.ToString("D"));

        if (!pt)
            qs = qs.Add(HttpNames.PagingTakeQueryStringName, paging.Take.ToString("D"));

        return qs;
    }
}