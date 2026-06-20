namespace Contoso.Shopping.Application.Policies;

public class ProductPolicy(IProductAdapter productAdapter)
{
    private static readonly LText _quantityText = new("Quantity");
    private readonly IProductAdapter _productAdapter = productAdapter.ThrowIfNull();

    public Task<Result<Product>> EnsureExistsAsync(string productId) => Result
        .GoAsync(() => _productAdapter.GetAsync(productId))
        .OnFailure(r => r.IsNotFoundError ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(productId), "Product was not found.")) : r);
}