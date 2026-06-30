namespace Contoso.Shopping.Test.Unit.Policies;

public class ProductPolicyTests : WithGenericTester<EntryPoint>
{
    private readonly Mock<IProductAdapter> _productAdapterMock = new();

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _productAdapterMock.Setup(x => x.GetAsync("existing-product-id"))
            .ReturnsAsync(Result.Go(new Product { Id = "existing-product-id", Sku = "WIDGET", Text = "Widget", UnitOfMeasureCode = "EA", Price = 9.99m }));
        _productAdapterMock.Setup(x => x.GetAsync("nonexistent-product-id"))
            .ReturnsAsync(Result.NotFoundError());
    }

    [Test]
    public void ProductPolicy_EnsureExistsAsync_ProductNotFound_ReturnsValidationError() => Test.Scoped(async test =>
    {
        var policy = new ProductPolicy(_productAdapterMock.Object);
        var result = await policy.EnsureExistsAsync("nonexistent-product-id");
        result.IsFailure.Should().BeTrue();
        result.IsValidationError.Should().BeTrue();

        result.Error.As<ValidationException>().AssertErrors(new ApiError("productId", "Product was not found."));
    });

    [Test]
    public void ProductPolicy_EnsureExistsAsync_ProductExists_ReturnsSuccess() => Test.Scoped(async test =>
    {
        var policy = new ProductPolicy(_productAdapterMock.Object);
        var result = await policy.EnsureExistsAsync("existing-product-id");
        result.IsSuccess.Should().BeTrue();
    });
}
