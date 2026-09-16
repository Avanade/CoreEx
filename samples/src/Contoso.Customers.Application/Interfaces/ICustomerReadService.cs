namespace Contoso.Customers.Application.Interfaces;

public interface ICustomerReadService
{
    Task<Contracts.Customer?> GetAsync(string id, CancellationToken ct = default);

    Task<ItemsResult<CustomerLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);

    Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default);
}
