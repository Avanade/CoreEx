namespace Contoso.Products.Application.Repositories;

/// <summary>Provides query and schema operations for the Products read model.</summary>
public interface IProductQueryRepository
{
    /// <summary>Gets the query schema supported by the Products read model.</summary>
    Task<JsonElement> QuerySchemaAsync(CancellationToken cancellationToken = default);

    /// <summary>Queries products using the supplied filters, search intent, and paging options.</summary>
    Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken = default);
}
