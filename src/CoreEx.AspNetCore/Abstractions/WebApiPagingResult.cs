namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Provides a <see cref="PagingResult"/> with the actual <see cref="PagedCount"/>.
/// </summary>
internal sealed record class WebApiPagingResult : PagingResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiPagingResult"/> class.
    /// </summary>
    /// <param name="paging">The <see cref="PagingResult"/>.</param>
    /// <param name="pagedCount">The actual count of the paged elements.</param>
    public WebApiPagingResult(PagingResult paging, int pagedCount) : base(paging)
    {
        WithTotalCount(paging.TotalCount);
        PagedCount = pagedCount;
    }

    /// <summary>
    /// Gets the actual count of the paged elements.
    /// </summary>
    public int PagedCount { get; }

    /// <summary>
    /// Gets the previous <see cref="PagingArgs"/>.
    /// </summary>
    /// <returns>The previous <see cref="PagingArgs"/> where applicable; otherwise, <see langword="null"/>.</returns>
    public PagingArgs? GetPreviousPage()
    {
        if (Skip == 0)
            return null;

        int skip = Skip - Take;
        if (TotalCount is not null && skip >= TotalCount)
            skip = (int)TotalCount - Take;

        if (skip < 0)
            skip = 0;

        return new PagingArgs(skip, Take > Skip ? Skip : Take);
    }

    /// <summary>
    /// Gets the next <see cref="PagingArgs"/>.
    /// </summary>
    /// <returns>The next <see cref="PagingArgs"/> where applicable; otherwise, <see langword="null"/>.</returns>
    public PagingArgs? GetNextPage()
    {
        if (PagedCount < Take)
            return null;

        if (TotalCount is not null && (Skip + Take) >= TotalCount)
            return null;

        return new PagingArgs(Skip + Take, Take);
    }
}