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

    private readonly Func<Type, QueryArgsConfig?>? _configSelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataQuery"/> class using <see cref="ReferenceDataQueryArgsConfig.Default"/> for all types.
    /// </summary>
    public ReferenceDataQuery() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataQuery"/> class with a per-type <see cref="QueryArgsConfig"/> selector.
    /// </summary>
    /// <param name="configSelector">Selects the <see cref="QueryArgsConfig"/> for a given <see cref="IReferenceData"/> <see cref="Type"/>; return <see langword="null"/> to fall back to <see cref="ReferenceDataQueryArgsConfig.Default"/>.</param>
    public ReferenceDataQuery(Func<Type, QueryArgsConfig?> configSelector)
        => _configSelector = configSelector.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<ItemsResult<TRef>> QueryAsync<TRef>(ReferenceDataOrchestrator referenceDataOrchestrator, QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken) where TRef : IReferenceData
    {
        var (items, p, totalCount) = await ExecuteQueryAsync(referenceDataOrchestrator, typeof(TRef), query, paging, cancellationToken).ConfigureAwait(false);
        var ir = new ItemsResult<TRef>(items.Cast<TRef>(), p);
        if (totalCount.HasValue) ir.WithTotalCount(totalCount.Value);
        return ir;
    }

    /// <inheritdoc/>
    public async Task<IItemsResult> QueryAsync(ReferenceDataOrchestrator referenceDataOrchestrator, Type refType, QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken)
    {
        var (items, p, totalCount) = await ExecuteQueryAsync(referenceDataOrchestrator, refType, query, paging, cancellationToken).ConfigureAwait(false);
        var ir = new ItemsResult<IReferenceData>(items, p);
        if (totalCount.HasValue) ir.WithTotalCount(totalCount.Value);
        return ir;
    }

    /// <summary>
    /// Executes the query proper.
    /// </summary>
    private async Task<(IReferenceData[] items, PagingArgs p, long? totalCount)> ExecuteQueryAsync(ReferenceDataOrchestrator referenceDataOrchestrator, Type refType, QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken)
    {
        var config = _configSelector?.Invoke(refType.ThrowIfNull()) ?? ReferenceDataQueryArgsConfig.Default;
        var parsed = config.Parse(query).ThrowOnError();
        var coll = await referenceDataOrchestrator.ThrowIfNull().GetByTypeRequiredAsync(refType, cancellationToken).ConfigureAwait(false);
        var items = query is not null && query.IsIncludeInactive ? coll.AllItems : coll.ActiveItems;
        var q = items.AsQueryable().Where(parsed).OfType<IReferenceData>();
        var p = paging ?? PagingArgs.None;
        long? totalCount = p.IsCountRequested ? q.Count() : null;
        return ([.. q.OrderBy(parsed).WithPaging(p)], p, totalCount);
    }
}
