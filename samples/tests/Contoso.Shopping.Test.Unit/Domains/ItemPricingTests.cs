namespace Contoso.Shopping.Test.Unit.Domains;

public class ItemPricingTests : WithGenericTester<EntryPoint>
{
    [Test]
    public void ItemPricing_NullInternalValue_RemainsArgumentError() => Test.Scoped(test =>
    {
        // Simulates programming misuse; the null-forgiving operator only bypasses compile-time checking.
        Action act = () => _ = new Domain.ValueObjects.ItemPricing { UnitOfMeasure = null! };
        act.Should().Throw<ArgumentNullException>();
    });

    [TestCase(true, "negative-unit-price", "Unit price cannot be negative.")]
    [TestCase(false, "negative-quantity", "Quantity cannot be negative.")]
    public void ItemPricing_NegativeValue_ThrowsBusinessException(bool price, string code, string message) => Test.Scoped(test =>
    {
        Action act = () => _ = new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", UnitPrice = price ? -1 : 1, Quantity = price ? 1 : -1 };
        act.Should().Throw<BusinessException>().WithMessage(message).Which.ErrorCode.Should().Be(code);
    });

    [TestCase(false)]
    [TestCase(true)]
    public void ItemPricing_InvalidUnitOfMeasure_ThrowsBusinessException(bool inactive) => Test.Scoped(test =>
    {
        UnitOfMeasure unit = inactive ? new UnitOfMeasure { Code = "OLD", IsInactive = true } : "UNKNOWN";
        Action act = () => _ = new Domain.ValueObjects.ItemPricing { UnitOfMeasure = unit };
        act.Should().Throw<BusinessException>().WithMessage("Pricing requires a valid, active unit of measure.").Which.ErrorCode.Should().Be("invalid-unit-of-measure");
    });

    [Test]
    public void ItemPricing_QuantityScale_ThrowsBusinessException() => Test.Scoped(test =>
    {
        var pricing = new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", UnitPrice = 10, Quantity = 1.5m };
        Action act = () => pricing.EnsureIsValid();
        act.Should().Throw<BusinessException>().WithMessage("Quantity decimal places exceed the specified unit-of-measure (Each) configuration of 0.").Which.ErrorCode.Should().Be("quantity-scale-exceeded");

        Action create = () => Domain.BasketItem.CreateNew("product", "sku", "Product", pricing);
        create.Should().Throw<BusinessException>().Which.ErrorCode.Should().Be("quantity-scale-exceeded");
    });

    [Test]
    public void ItemPricing_ValidValues_PreserveTotalsAndCopyGuards() => Test.Scoped(test =>
    {
        var pricing = new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "HR", UnitPrice = 10, Quantity = 1.25m }.EnsureIsValid();
        pricing.Total.Should().Be(12.5m);
        var zero = new Domain.ValueObjects.ItemPricing { UnitOfMeasure = "EA", UnitPrice = 0, Quantity = 0 }.EnsureIsValid();
        zero.Total.Should().Be(0);

        Action copy = () => _ = pricing with { Quantity = -1 };
        copy.Should().Throw<BusinessException>().Which.ErrorCode.Should().Be("negative-quantity");
        pricing.Quantity.Should().Be(1.25m);
    });
}
