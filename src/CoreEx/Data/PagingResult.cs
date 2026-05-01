namespace CoreEx.Data;

/// <summary>
/// Represents the resulting paging response including <see cref="TotalCount"/>.
/// </summary>
public record class PagingResult : PagingArgs, ITotalCount
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagingResult"/> class with the specified <see cref="PagingArgs"/>.
    /// </summary>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    public PagingResult(PagingArgs? paging = null)
    {
        Skip = paging?.Skip ?? 0;
        Take = paging?.Take ?? DefaultTake;
        IsCountRequested = paging?.IsCountRequested ?? false;
    }

    /// <inheritdoc/>
    public long? TotalCount { get; private set; }

    /// <inheritdoc/>
    void ITotalCount.WithTotalCount(long? totalCount) => WithTotalCount(totalCount);

    /// <summary>
    /// Sets the <see cref="TotalCount"/> of the elements in the sequence.
    /// </summary>
    /// <param name="totalCount">The total count of the elements in the sequence.</param>
    /// <returns>The <see cref="PagingResult"/> to support fluent-style method-chaining.</returns>
    public PagingResult WithTotalCount(long? totalCount)
    {
        TotalCount = totalCount is null || totalCount.Value < 0 ? null : totalCount.Value;
        return this;
    }
}