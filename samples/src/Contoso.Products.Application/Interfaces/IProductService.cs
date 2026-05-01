namespace Contoso.Products.Application.Interfaces;

public interface IProductService
{
    Task<Contracts.Product?> GetAsync(string id);

    Task<Contracts.Product> CreateAsync(Contracts.Product product);

    Task<Contracts.Product> UpdateAsync(Contracts.Product product);

    Task<Contracts.Product> ActivateAsync(string id);

    Task<Contracts.Product> DeactivateAsync(string id);

    Task DeleteAsync(string id);
}