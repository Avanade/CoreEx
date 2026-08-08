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

        // Perform the arithmetic in long (Skip/Take are int, but TotalCount is long? and can legitimately exceed int.MaxValue); narrow to int only once, at the end, clamped -
        // narrowing TotalCount to int before subtracting (as this used to) silently wraps for values above int.MaxValue, producing a nonsensical Skip.
        long skip = Skip - Take;
        if (TotalCount is not null && skip >= TotalCount)
            skip = TotalCount.Value - Take;

        if (skip < 0)
            skip = 0;

        return new PagingArgs((int)Math.Min(skip, int.MaxValue), Take > Skip ? Skip : Take);
    }

    /// <summary>
    /// Gets the next <see cref="PagingArgs"/>.
    /// </summary>
    /// <returns>The next <see cref="PagingArgs"/> where applicable; otherwise, <see langword="null"/>.</returns>
    public PagingArgs? GetNextPage()
    {
        if (PagedCount < Take)
            return null;

        // Perform the arithmetic in long - Skip has no upper bound (unlike Take, which is clamped to PagingArgs.MaximumTake) and can be supplied directly by the caller/client
        // via the query string, so Skip + Take can overflow int and silently wrap (typically negative) if computed as int.
        long nextSkip = (long)Skip + Take;
        if (TotalCount is not null && nextSkip >= TotalCount)
            return null;

        return new PagingArgs((int)Math.Min(nextSkip, int.MaxValue), Take);
    }
}