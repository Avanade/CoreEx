namespace Contoso.Shopping.Domain.ValueObjects;

public sealed record class ItemPricing
{
    public required Contracts.UnitOfMeasure UnitOfMeasure { get; init => field = value.ThrowIfInactive(); }

    public decimal UnitPrice { get; init => field = value.ThrowIfLessThanZero(); } 

    public decimal Quantity { get; init => field = value.ThrowIfLessThanZero(); }

    public decimal Total => UnitPrice * Quantity;

    public ItemPricing EnsureIsValid() => DecimalRuleHelper.CheckScale(Quantity, UnitOfMeasure.Scale) ? this
        : throw new ValidationException($"Quantity decimal places exceed the specified unit-of-measure ({UnitOfMeasure.Text}) configuration of {UnitOfMeasure.Scale}.");
}