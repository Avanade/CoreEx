namespace CoreEx.RefData.Abstractions;

/// <summary>
/// Provides the default implementation of the <see cref="IReferenceDataQuery"/> interface.
/// </summary>
public sealed class ReferenceDataQuery : IReferenceDataQuery
{
    /// <summary>
    /// Gets the default <see cref="ReferenceDataQuery"/> instance.
    /// </summary>
    public static ReferenceDataQuery Default { get; } = new ReferenceDataQuery();

    /// <inheritdoc/>
    public async Task<ItemsResult<TRef>> QueryAsync<TRef>(ReferenceDataOrchestrator referenceDataOrchestrator, QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken) where TRef : IReferenceData
    {
        var parsed = ReferenceDataQueryArgsConfig.Default.Parse(query).ThrowOnError();
        var coll = await referenceDataOrchestrator.ThrowIfNull().GetByTypeRequiredAsync<TRef>(cancellationToken).ConfigureAwait(false);
        var items = query is not null && query.IsIncludeInactive ? coll.AllItems : coll.ActiveItems;
        var q = items.AsQueryable().Where(parsed).OfType<TRef>();

        var ir = new ItemsResult<TRef>(paging)
        {
            Items = [.. q.OrderBy(parsed).WithPaging(paging)]
        };

        if (paging.IsCountRequested)
            ir.WithTotalCount(q.Count());

        return ir;
    }
}
