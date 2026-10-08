namespace Contoso.Shopping.Application.Adapters.Customers;

/// <summary>
/// Enables the Customers domain integration, serving as the external dependency boundary (anti-corruption layer) for customer-related operations.
/// </summary>
/// <remarks>Unlike the <see cref="Products.IProductAdapter"/> (which reads from an event-replicated local store), this adapter makes real-time calls to the Customers domain.</remarks>
public interface ICustomerAdapter
{
    /// <summary>
    /// Gets the customer; will result in a <see cref="NotFoundException"/> where it does not exist.
    /// </summary>
    Task<Result<Customer>> GetAsync(string id, CancellationToken ct = default);
}
