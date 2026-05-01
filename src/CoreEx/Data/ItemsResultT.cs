namespace CoreEx.Data;

/// <summary>
/// Provides an <see cref="IItemsResult{TItem}"/> that supports <see cref="Items"/> and corresponding <see cref="Paging"/>.
/// </summary>
/// <typeparam name="TItem">The underlying entity <see cref="Type"/>.</typeparam>
/// <remarks>Generally an <see cref="IItemsResult"/> is not intended to be serialized and returned as <see cref="HttpResponseMessage"/> content; 
/// the underlying <see cref="Items"/> should be serialized with the <see cref="IItemsResult.Paging"/> returned as <see cref="HttpResponseMessage.Headers"/>.
/// <para>Use <see cref="PagingArgs.None"/> to specify that no paging was requested/applied.</para></remarks>
public sealed record class ItemsResult<TItem> : IItemsResult<TItem>, ITotalCount
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsResult{TItem}"/> class defaulting the <see cref="Paging"/>.
    /// </summary>
    public ItemsResult() : this(new PagingArgs()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsResult{TItem}"/> class with <paramref name="paging"/> (defaults where <see langword="null"/>).
    /// </summary>
    /// <param name="paging">Defaults the <see cref="IItemsResult.Paging"/> to the specified <see cref="PagingArgs"/>.</param>
    public ItemsResult(PagingArgs? paging)
    {
        if (paging is null || !paging.IsNone)
            Paging = new PagingResult(paging);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemsResult{TItem}"/> class with the specified <paramref name="items"/> and optional <paramref name="paging"/>.
    /// </summary>
    /// <param name="items">The initial items.</param>
    /// <param name="paging">Defaults the <see cref="IItemsResult.Paging"/> to the requesting <see cref="PagingArgs"/>.</param>
    public ItemsResult(IEnumerable<TItem>? items, PagingArgs? paging = null) : this(paging ?? PagingArgs.None) => Items = items;

    /// <inheritdoc/>
    public IEnumerable<TItem>? Items { get; set => field = field is null ? value : throw new InvalidOperationException($"{nameof(Items)} can only be set once."); }

    /// <inheritdoc/>
    public PagingResult? Paging { get; private set; }

    /// <summary>
    /// Indicates whether the total count has been requested.
    /// </summary>
    private bool IsCountRequested => Paging is not null && Paging.IsCountRequested;

    /// <inheritdoc/>
    bool ITotalCount.IsCountRequested => IsCountRequested;

    /// <inheritdoc/>
    long? ITotalCount.TotalCount => IsCountRequested ? Paging?.TotalCount : null;

    /// <inheritdoc/>
    void ITotalCount.WithTotalCount(long? totalCount) => Paging.AdjustWhen(_ => IsCountRequested, p => p.WithTotalCount(totalCount));
}