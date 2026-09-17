namespace Contoso.Customers.Infrastructure.Repositories;

[ScopedService<ICustomerRepository>]
public class CustomerRepository(CustomersCosmosDb cosmos) : ICustomerRepository
{
    private readonly CustomersCosmosDb _cosmos = cosmos.ThrowIfNull();

    public Task<Contracts.Customer?> GetAsync(string id, CancellationToken ct = default) => _cosmos.Customers.GetAsync(CompositeKey.Create(id), ct);

    public Task<DataResult<Contracts.Customer>> CreateAsync(Contracts.Customer customer, CancellationToken ct = default) => _cosmos.Customers.CreateAsync(customer, ct);

    public Task<DataResult<Contracts.Customer>> UpdateAsync(Contracts.Customer customer, CancellationToken ct = default) => _cosmos.Customers.UpdateAsync(customer, ct);

    public Task<DataResult> DeleteAsync(string id, CancellationToken ct = default) => _cosmos.Customers.DeleteAsync(CompositeKey.Create(id), ct);

    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => Task.FromResult(CustomerQueryArgsConfig.Default.ToJsonSchema());

    public async Task<ItemsResult<Contracts.CustomerLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
    {
        var parsed = CustomerQueryArgsConfig.Default.Parse(query).ThrowOnError();

        return await _cosmos.Customers.Container
            .Query(q => q.Where(parsed).OrderBy(parsed))
            .WithPaging(paging)
            .ToMappedItemsResultAsync(m => new Contracts.CustomerLite
            {
                Id = m.Id,
                FirstName = m.FirstName,
                LastName = m.LastName,
                Email = m.Email,
                CustomerTypeCode = m.CustomerTypeCode
            }, cancellationToken: ct);
    }
}
