namespace Contoso.Customers.Infrastructure.Repositories;

[ScopedService<ICustomerRepository>]
public class CustomerRepository(CustomersCosmosDb cosmos) : ICustomerRepository
{
    private readonly CustomersCosmosDb _cosmos = cosmos.ThrowIfNull();
    private readonly CosmosDbMappedContainer<Contracts.Customer, Persistence.Customer, CustomerMapper> _mapped = cosmos.Customers.ToMappedModel<Contracts.Customer, CustomerMapper>(new CustomerMapper());

    // PartitionKey == Id (kept simple, per explicit direction), so every point operation below can use an efficient point-read/write directly from the id alone - no lookup required.
    public Task<Contracts.Customer?> GetAsync(string id, CancellationToken ct = default) => _mapped.GetAsync(CompositeKey.Create(id), new PartitionKey(id), ct);

    public Task<DataResult<Contracts.Customer>> CreateAsync(Contracts.Customer customer, CancellationToken ct = default) => _mapped.CreateAsync(customer, ct);

    public Task<DataResult<Contracts.Customer>> UpdateAsync(Contracts.Customer customer, CancellationToken ct = default) => _mapped.UpdateAsync(customer, ct);

    public Task<DataResult> DeleteAsync(string id, CancellationToken ct = default) => _mapped.DeleteAsync(CompositeKey.Create(id), new PartitionKey(id), ct);

    public Task<JsonElement> QuerySchemaAsync(CancellationToken ct = default) => Task.FromResult(CustomerQueryArgsConfig.Default.ToJsonSchema());

    public async Task<ItemsResult<Contracts.CustomerLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken ct = default)
    {
        var parsed = CustomerQueryArgsConfig.Default.Parse(query).ThrowOnError();

        return await _cosmos.Customers
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
