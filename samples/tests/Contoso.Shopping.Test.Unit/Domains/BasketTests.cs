namespace Contoso.Shopping.Test.Unit.Domains;

public class BasketTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Basket_ApplyDiscount_Success() => Test.Scoped(test =>
    {
        // Arrange: Create a basket with an item.
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.Active, null,
            [Domain.BasketItem.CreateFrom("item-id", "product-id", "sku", "text", new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", Quantity = 1, UnitPrice = 100m }, null)],
            null, null);

        // Act: Apply a discount coupon to the basket.
        basket.ApplyDiscount(new DiscountCoupon { Code = "DISCOUNT10", DiscountPercentage = 10m });

        // Assert: Verify that the discount has been applied correctly.
        basket.DiscountAmount.Should().Be(10m);
        basket.Total.Should().Be(90m);
    });

    [Test]
    public void Basket_ApplyDiscount_Invalid_Status() => Test.Scoped(test =>
    {
        // Arrange: Create a basket with an item.
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.CheckedOut, null,
            [Domain.BasketItem.CreateFrom("item-id", "product-id", "sku", "text", new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", Quantity = 1, UnitPrice = 100m }, null)],
            null, null);

        // Act: Apply a discount coupon to the basket.
        Action act = () => basket.ApplyDiscount(new DiscountCoupon { Code = "DISCOUNT10", DiscountPercentage = 10m });

        // Assert: Verify that the discount can not be applied.
        act.Should().Throw<BusinessException>().WithMessage("Basket has a status of 'Checked-out' and as such cannot be modified.");

        basket.DiscountAmount.Should().Be(0m);
        basket.Total.Should().Be(100m);
    });
}
