namespace Contoso.Customers.Application.Interfaces;

public interface ICustomerService
{
    Task<Contracts.Customer?> GetAsync(string id, CancellationToken ct = default);

    Task<Contracts.Customer> CreateAsync(Contracts.Customer customer, CancellationToken ct = default);

    Task<Contracts.Customer> UpdateAsync(Contracts.Customer customer, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Flags the customer (idempotently) as having shopped, blocking any future delete.
    /// </summary>
    Task MarkAsShoppedAsync(string id, CancellationToken ct = default);
}
