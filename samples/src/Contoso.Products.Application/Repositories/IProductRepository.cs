namespace Contoso.Products.Application.Repositories;

public interface IProductRepository
{
    Task<Contracts.Product?> GetAsync(string id, CancellationToken cancellationToken = default);

    Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product, CancellationToken cancellationToken = default);

    Task<DataResult<Contracts.Product>> UpdateAsync(Contracts.Product product, CancellationToken cancellationToken = default);

    Task<DataResult> DeleteAsync(string id, CancellationToken cancellationToken = default);

    Task<Dictionary<string, Contracts.ProductReserve>> GetForReservationAsync(string[] ids, CancellationToken cancellationToken = default);
}
