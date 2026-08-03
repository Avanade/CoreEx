namespace Contoso.Shopping.Test.Unit.Domains;

public class BasketTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void Basket_ApplyDiscount_Success() => Test.Scoped(test =>
    {
        // Arrange: Create a basket with an item.
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.Active, null, null,
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
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.CheckedOut, null, null,
            [Domain.BasketItem.CreateFrom("item-id", "product-id", "sku", "text", new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", Quantity = 1, UnitPrice = 100m }, null)],
            null, null);

        // Act: Apply a discount coupon to the basket.
        Action act = () => basket.ApplyDiscount(new DiscountCoupon { Code = "DISCOUNT10", DiscountPercentage = 10m });

        // Assert: Verify that the discount can not be applied.
        act.Should().Throw<BusinessException>().WithMessage("Basket has a status of 'Checked-out' and as such cannot be modified.");

        basket.DiscountAmount.Should().Be(0m);
        basket.Total.Should().Be(100m);
    });

    [Test]
    public void Basket_UpdateShippingAddress_Success() => Test.Scoped(test =>
    {
        // Arrange: Create a basket with no shipping address.
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.Active, null, null, [], null, null);
        var address = new Domain.ValueObjects.Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" };

        // Act: Set a shipping address.
        basket.UpdateShippingAddress(address);

        // Assert: Verify the address is set and the basket is marked as modified.
        basket.ShippingAddress.Should().Be(address);
        basket.HasChanges.Should().BeTrue();
    });

    [Test]
    public void Basket_UpdateShippingAddress_Clear() => Test.Scoped(test =>
    {
        // Arrange: Create a basket with an existing shipping address.
        var existingAddress = new Domain.ValueObjects.Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" };
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.Active, null, existingAddress, [], null, null);

        // Act: Clear the shipping address.
        basket.UpdateShippingAddress(null);

        // Assert: Verify the address is cleared and the basket is marked as modified.
        basket.ShippingAddress.Should().BeNull();
        basket.HasChanges.Should().BeTrue();
    });

    [Test]
    public void Basket_UpdateShippingAddress_NoChange() => Test.Scoped(test =>
    {
        // Arrange: Create a basket with an existing shipping address.
        var address = new Domain.ValueObjects.Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" };
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.Active, null, address, [], null, null);

        // Act: Update with a structurally equal address — record value equality prevents mutation.
        basket.UpdateShippingAddress(new Domain.ValueObjects.Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" });

        // Assert: Verify no modification occurred.
        basket.ShippingAddress.Should().Be(address);
        basket.HasChanges.Should().BeFalse();
    });

    [Test]
    public void Basket_UpdateShippingAddress_Invalid_Status() => Test.Scoped(test =>
    {
        // Arrange: Create a checked-out basket.
        var basket = Domain.Basket.CreateFrom("basket-id", "customer-id", BasketStatus.CheckedOut, null, null, [], null, null);

        // Act: Attempt to set a shipping address.
        Action act = () => basket.UpdateShippingAddress(new Domain.ValueObjects.Address { Street1 = "1 Main St", City = "Sydney", PostCode = "2000", State = "NSW" });

        // Assert: Verify that the update is rejected and the address remains unset.
        act.Should().Throw<BusinessException>().WithMessage("Basket has a status of 'Checked-out' and as such cannot be modified.");
        basket.ShippingAddress.Should().BeNull();
    });
}
