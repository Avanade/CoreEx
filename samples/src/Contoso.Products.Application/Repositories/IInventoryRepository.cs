namespace Contoso.Products.Application.Repositories;

public interface IInventoryRepository
{
    Task<decimal?> GetOnHandAsync(string productId, bool throwNotFoundException);
}