namespace CoreEx.RefData.Abstractions;

/// <summary>
/// Defines the interface for a <see cref="ReferenceDataOrchestrator"/> query.
/// </summary>
public interface IReferenceDataQuery
{
    /// <summary>
    /// Queries the <see cref="ReferenceDataOrchestrator"/> for the specified <typeparamref name="TRef"/> type using the <paramref name="query"/> and <paramref name="paging"/>.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <param name="referenceDataOrchestrator">The <see cref="ReferenceDataOrchestrator"/> source.</param>
    /// <param name="query">The <see cref="QueryArgs"/>.</param>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ItemsResult{TItem}"/> of <typeparamref name="TRef"/>.</returns>
    Task<ItemsResult<TRef>> QueryAsync<TRef>(ReferenceDataOrchestrator referenceDataOrchestrator, QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken) where TRef : IReferenceData;
}
