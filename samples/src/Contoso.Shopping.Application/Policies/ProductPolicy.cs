namespace Contoso.Shopping.Application.Policies;

public class ProductPolicy(IProductAdapter productAdapter)
{
    private static readonly LText _productText = new("Product");
    private readonly IProductAdapter _productAdapter = productAdapter.ThrowIfNull();

    public Task<Result<Product>> EnsureExistsAsync(string productId, CancellationToken ct = default) => Result
        .GoAsync(() => _productAdapter.GetAsync(productId, ct))
        .OnFailure(r => r.IsNotFoundError ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(productId), "{0} was not found.", _productText)) : r);
}
