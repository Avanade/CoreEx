namespace Contoso.Shopping.Domain.ValueObjects;

public sealed record class ItemPricing
{
    public required Contracts.UnitOfMeasure UnitOfMeasure
    {
        get;
        init
        {
            value.ThrowIfNull();
            if (!value.IsValid || !value.IsActive)
                throw new BusinessException("Pricing requires a valid, active unit of measure.").WithErrorCode("invalid-unit-of-measure");

            field = value;
        }
    }

    public decimal UnitPrice { get; init => field = value < 0 ? throw new BusinessException("Unit price cannot be negative.").WithErrorCode("negative-unit-price") : value; }

    public decimal Quantity { get; init => field = value < 0 ? throw new BusinessException("Quantity cannot be negative.").WithErrorCode("negative-quantity") : value; }

    public decimal Total => UnitPrice * Quantity;

    public ItemPricing EnsureIsValid() => DecimalRuleHelper.CheckScale(Quantity, UnitOfMeasure.Scale) ? this
        : throw new BusinessException($"Quantity decimal places exceed the specified unit-of-measure ({UnitOfMeasure.Text}) configuration of {UnitOfMeasure.Scale}.").WithErrorCode("quantity-scale-exceeded");
}
