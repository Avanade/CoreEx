namespace Contoso.Products.Application.Interfaces;

public interface IInventoryService
{
    Task<decimal> GetOnHandAsync(string productId, CancellationToken ct = default);
}