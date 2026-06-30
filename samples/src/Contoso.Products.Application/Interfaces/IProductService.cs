namespace Contoso.Products.Application.Interfaces;

public interface IProductService
{
    Task<Contracts.Product?> GetAsync(string id, CancellationToken ct = default);

    Task<Contracts.Product> CreateAsync(Contracts.Product product, CancellationToken ct = default);

    Task<Contracts.Product> UpdateAsync(Contracts.Product product, CancellationToken ct = default);

    Task<Contracts.Product> ActivateAsync(string id, CancellationToken ct = default);

    Task<Contracts.Product> DeactivateAsync(string id, CancellationToken ct = default);

    Task DeleteAsync(string id, CancellationToken ct = default);
}