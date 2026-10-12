namespace Contoso.Shopping.Infrastructure.Adapters.Customers;

[ScopedService<ICustomerAdapter>]
public class CustomerAdapter(CustomersHttpClient client, IHybridCache cache) : ICustomerAdapter
{
    private readonly CustomersHttpClient _client = client.ThrowIfNull();
    private readonly IHybridCache _cache = cache.ThrowIfNull();

    // Customer data is owned by another domain and can change at any time, so keep the expirations short; overridable via configuration (CoreEx:Caching:Customer:*).
    private readonly HybridCacheEntryOptions _cacheOptions = HybridCacheEntryOptions.CreateFor<Customer>(localExpiration: TimeSpan.FromMinutes(1), distributedExpiration: TimeSpan.FromMinutes(5));

    /// <inheritdoc/>
    /// <remarks>Invokes the Customers API in real-time (there is no local replicated copy of customer data); the result is cached to avoid repeated cross-domain calls. Only a successful result is cached,
    /// so a customer that does not exist (yet) is never remembered as such.</remarks>
    public Task<Result<Customer>> GetAsync(string id, CancellationToken ct = default) => _cache.GetOrCreateWithResultAsync<Customer>(id, c => _client.GetAsync(id, c), _cacheOptions, ct);
}
