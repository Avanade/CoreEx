namespace Contoso.Customers.Application.Repositories;

public interface ICustomerRepository
{
    Task<Contracts.Customer?> GetAsync(string id, CancellationToken ct = default);

    Task<DataResult<Contracts.Customer>> CreateAsync(Contracts.Customer customer, CancellationToken ct = default);

    Task<DataResult<Contracts.Customer>> UpdateAsync(Contracts.Customer customer, CancellationToken ct = default);

    Task<DataResult> DeleteAsync(string id, CancellationToken ct = default);

    Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default);

    Task<ItemsResult<Contracts.CustomerLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default);
}
