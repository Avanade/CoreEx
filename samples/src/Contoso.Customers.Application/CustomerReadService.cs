namespace Contoso.Customers.Application;

[ScopedService<ICustomerReadService>]
public class CustomerReadService(ICustomerRepository repository) : ICustomerReadService
{
    private readonly ICustomerRepository _repository = repository.ThrowIfNull();

    public Task<Customer?> GetAsync(string id, CancellationToken ct = default) => _repository.GetAsync(id, ct);

    public Task<ItemsResult<CustomerLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default) => _repository.QueryAsync(query, paging, ct);

    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => _repository.QuerySchemaAsync(ct);
}
